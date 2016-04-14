using System.Collections.Generic;
using System.Collections.Specialized;
using RabbitMQ.Client;
using STCLINE.KP50.Global;

namespace Bars.KP50.Queue
{
    using System;
    using System.Data;

    using Bars.KP50.Utils;
    using Bars.QueueCore;
    using Bars.RabbitMq;

    using Globals.SOURCE.Config;
    using Globals.SOURCE.Container;

    using IBM.Data.Informix;

    using Newtonsoft.Json;

    using Npgsql;

    using STCLINE.KP50.DataBase;

    /// <summary>Провайдер работ</summary>
    public class JobProvider : IJobProvider
    {
        private static readonly object LockObject = new object();
        private static bool init;

        /// <summary>Добавить работу на выполнение</summary>
        /// <param name="jobType">Тип работы</param>
        /// <param name="jobArguments">Параметры запуска работы</param>
        /// <param name="queueName">Название очереди</param>
        public void AddJob(JobType jobType, JobArguments jobArguments, string queueName)
        {
            Init();

            var mainDbParams = IocContainer.Current.Resolve<IConfigProvider>().GetConfig().MainDbParams;
            IDbConnection connection;
            using (connection = DBManager.GetConnection(mainDbParams.ConnectionString))
            {
                try
                {
                    connection.Open();
                    var command = connection is NpgsqlConnection
                        ? GetPostgreInsertCommand(connection, jobType, jobArguments)
                        : GetInformixInsertCommand(connection, jobType, jobArguments);

                    command.ExecuteNonQuery();

                    jobArguments.JobId = DBManager.GetSerialValue(connection);

                    var host = IocContainer.Current.Resolve<IConfigProvider>().GetConfig().QueueHost;
                    if (string.IsNullOrEmpty(host))
                    {
                        throw new ApplicationException("Не удалось получить адрес сервера очередей. Проверьте настройки приложения.");
                    }

                    using (var producer = new Producer(host, queueName))
                    {
                        producer.SendMessage(jobArguments);
                    }
                }
                finally
                {
                    if (connection != null)
                    {
                        connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Остановить выполнение работы
        /// </summary>
        /// <param name="jobId">работа</param>
        public void StopJob(int jobId)
        {
            var host = IocContainer.Current.Resolve<IConfigProvider>().GetConfig().QueueHost;
            if (string.IsNullOrEmpty(host))
            {
                throw new ApplicationException("Не удалось получить адрес сервера очередей. Проверьте настройки приложения.");
            }
            using (var producer = new ProducerExchange(host, "service-exchange", ExchangeType.Fanout))
            {
                if (!producer.ConnectToRabbitMQ())
                {
                    MonitorLog.WriteLog("Сервер недоступен!", MonitorLog.typelog.Error, false);
                    return;
                }
                producer.SendMessage(new JobSystemArguments(jobId, JobActions.Stop));
            }
        }

        /// <summary>
        /// Перезапустить работу
        /// </summary>
        /// <param name="jobId">работа</param>
        public void RestartJob(int jobId)
        {
            var host = IocContainer.Current.Resolve<IConfigProvider>().GetConfig().QueueHost;
            if (string.IsNullOrEmpty(host))
            {
                throw new ApplicationException("Не удалось получить адрес сервера очередей. Проверьте настройки приложения.");
            }
            using (var producer = new ProducerExchange(host, "service-exchange", ExchangeType.Fanout))
            {
                if (!producer.ConnectToRabbitMQ())
                {
                    MonitorLog.WriteLog("Сервер недоступен!", MonitorLog.typelog.Error, false);
                    return;
                }
                producer.SendMessage(new JobSystemArguments(jobId, JobActions.Restart));
            }
        }

        private IDbCommand GetPostgreInsertCommand(IDbConnection connection, JobType jobType, JobArguments jobArguments)
        {
            var command = connection.CreateCommand();

            command.CommandText = @"insert into public.jobs(job_state, job_type, job_code, job_name, data, create_date, start_date, end_date, success, message)
values (:job_state, :job_type, :job_code, :job_name, :data, :create_date, :start_date, :end_date, :success, :message)";
            
            command.Parameters.Add(new NpgsqlParameter("job_state", (int)JobState.New));
            command.Parameters.Add(new NpgsqlParameter("job_type", (int)jobType));
            command.Parameters.Add(new NpgsqlParameter("job_code", jobArguments.Code));
            command.Parameters.Add(new NpgsqlParameter("job_name", jobArguments.Name));
            command.Parameters.Add(new NpgsqlParameter("data", JsonConvert.SerializeObject(NvcToDictionary(jobArguments.Parameters, true))));
            command.Parameters.Add(new NpgsqlParameter("create_date", DateTime.Now));
            command.Parameters.Add(new NpgsqlParameter("start_date", null));
            command.Parameters.Add(new NpgsqlParameter("end_date", null));
            command.Parameters.Add(new NpgsqlParameter("success", false));
            command.Parameters.Add(new NpgsqlParameter("message", null));

            return command;
        }

        private IDbCommand GetInformixInsertCommand(IDbConnection connection, JobType jobType, JobArguments jobArguments)
        {
            var command = connection.CreateCommand();
            
            command.CommandText = string.Format(@"insert into jobs(job_state, job_type, job_code, job_name, data, create_date, start_date, end_date, success, message)
                        values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");

            command.Parameters.Add(new IfxParameter("job_state", (int)JobState.New));
            command.Parameters.Add(new IfxParameter("job_type", (int)jobType));
            command.Parameters.Add(new IfxParameter("job_code", jobArguments.Code));
            command.Parameters.Add(new IfxParameter("job_name", jobArguments.Name));
            command.Parameters.Add(new IfxParameter("data", JsonConvert.SerializeObject(NvcToDictionary(jobArguments.Parameters, true))));
            command.Parameters.Add(new IfxParameter("create_date", DateTime.Now));
            command.Parameters.Add(new IfxParameter("start_date", null));
            command.Parameters.Add(new IfxParameter("end_date", null));
            command.Parameters.Add(new IfxParameter("success", false));
            command.Parameters.Add(new IfxParameter("message", null));

            return command;
        }

        /// <summary>Инициализация</summary>
        private void Init()
        {
#warning Удалить метод после введения нормального мигратора
            if (!init)
            {
                lock (LockObject)
                {
                    if (!init)
                    {
                        init = true;
                        var mainDbParams = IocContainer.Current.Resolve<IConfigProvider>().GetConfig().MainDbParams;

                        IDbConnection connection;
                        using (connection = DBManager.GetConnection(mainDbParams.ConnectionString))
                        {
                            try
                            {
                                connection.Open();

                                var command = connection.CreateCommand();

#if PG
                                command.CommandText = @"select count(1)
                                    from information_schema.tables
                                    where table_schema = 'public'
                                    and table_name = 'jobs'";
#else
                                command.CommandText = @"select count(1)
                                    from systables
                                    where tabname = 'jobs'
                                    and tabid > 99";
#endif

                                var result = command.ExecuteScalar().ToBool();
                                if (!result)
                                {
#if PG
                                    command.CommandText = @"CREATE TABLE public.jobs(
                                        id serial NOT NULL,
                                        job_state numeric(4,0) NOT NULL,
                                        job_type numeric(4,0) NOT NULL,
                                        job_code character varying NOT NULL,
                                        job_name character varying NOT NULL,
                                        data character varying,
                                        create_date timestamp without time zone NOT NULL,
                                        start_date timestamp without time zone,
                                        end_date timestamp without time zone,
                                        success boolean NOT NULL,
                                        message character varying
                                    )";
#else
                                    command.CommandText = @"CREATE TABLE jobs(
                                        id serial NOT NULL,
                                        job_state numeric(4,0) NOT NULL,
                                        job_type numeric(4,0) NOT NULL,
                                        job_code lvarchar NOT NULL,
                                        job_name lvarchar NOT NULL,
                                        data lvarchar,
                                        create_date datetime year to second NOT NULL,
                                        start_date datetime year to second time zone,
                                        end_date ditetime year to second time zone,
                                        success boolean NOT NULL,
                                        message lvarchar
                                    )";
#endif

                                    command.ExecuteNonQuery();
                                }
                            }
                            finally
                            {
                                if (connection != null)
                                {
                                    connection.Close();
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Из NameValueCollection в Dictionary для сериализации
        /// </summary>
        /// <param name="nvc">Входящий NameValueCollection</param>
        /// <param name="handleMultipleValuesPerKey">содержит несколько значения для одного ключа</param>
        /// <returns></returns>
        static Dictionary<string, object> NvcToDictionary(NameValueCollection nvc, bool handleMultipleValuesPerKey)
        {
            var result = new Dictionary<string, object>();
            foreach (string key in nvc.Keys)
            {
                if (handleMultipleValuesPerKey)
                {
                    var values = nvc.GetValues(key);
                    if (values != null && values.Length == 1)
                    {
                        result.Add(key, values[0]);
                    }
                    else
                    {
                        result.Add(key, values);
                    }
                }
                else
                {
                    result.Add(key, nvc[key]);
                }
            }

            return result;
        }
    }
}
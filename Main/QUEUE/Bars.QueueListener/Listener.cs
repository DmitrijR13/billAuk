using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;

namespace Bars.QueueListener
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    using Bars.QueueCore;
    using Bars.RabbitMq;

    using NLog;

    using RabbitMQ.Client;
    using RabbitMQ.Client.Exceptions;

    public class Listener : IDisposable
    {
        private readonly string _rabbitMqHost;

        public Listener(string rabbitMqHost, IEnumerable<string> processingQueues)
        {
            _rabbitMqHost = rabbitMqHost;

            ProcessingQueues = processingQueues.Select(x => new Queue(x)).ToList();
            QueueConsumers = new Dictionary<string, Consumer>();
            WorkerManager = new WorkersManager();
        }

        ~Listener()
        {
            Dispose(false);
        }

        public State State { get; set; }

        protected Dictionary<string, Consumer> QueueConsumers { get; set; }

        private IList<Queue> ProcessingQueues { get; set; }

        private WorkersManager WorkerManager { get; set; }

        public void Listen()
        {
            foreach (var queue in ProcessingQueues)
            {
                var consumer = new Consumer(_rabbitMqHost, queue.Name);
                consumer.StartConsuming();
                QueueConsumers.Add(queue.Name, consumer);
                consumer.OnMessageReceived += ConsumerOnMessageReceived;
            }

            var worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(UpdateStates);
            worker.RunWorkerAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (QueueConsumers == null)
            {
                return;
            }

            foreach (var consumer in QueueConsumers)
            {
                consumer.Value.Dispose();
            }

            QueueConsumers.Clear();
            QueueConsumers = null;
        }

        private void ConsumerOnMessageReceived(byte[] message, string queue)
        {
            var logger = LogManager.GetLogger("nlogger");
            logger.Info("Received message: queue: {0}", queue);
            var jodId = 0;
            var q = ProcessingQueues.First(x => x.Name == queue);
            try
            {
                JobArguments args;
                using (var memoryStream = new MemoryStream(message))
                {
                    var deserializer = new BinaryFormatter();
                    args = (JobArguments)deserializer.Deserialize(memoryStream);
                    jodId = args.JobId;
                }

                q.Start(args.JobId);

                WorkerManager.RunWork(args, q);

                q.Stop();
            }
            catch (Exception exc)
            {
                q.Stop();
                logger.ErrorException("Error processed message", exc);
                var connectionString = new NpgsqlConnectionStringBuilder()
                {
                    Host = "192.168.170.215",
                    Port = 5432,
                    UserName = "postgres",
                    Password = "postgres",
                    Database = "websmr"
                };
                using (var conn = new NpgsqlConnection(connectionString.ToString()))
                {
                    conn.Open();
                    var update = string.Format("UPDATE public.jobs SET job_state = {0}, end_date = now(), success = 'f', message = 'Ошибка получения отчета.' WHERE id = :id", JobState.End.GetHashCode());
                    using (var comm = new NpgsqlCommand(update, conn))
                    {
                        comm.Parameters.Add(new NpgsqlParameter("id", jodId));
                        comm.ExecuteNonQuery();
                    }
                }
                //throw new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Application, 1, "Error processed message"));
            }
        }

        /// <summary>
        /// Остановка задачи
        /// </summary>
        /// <param name="jobId">идентификатор задачи</param>
        /// <returns></returns>
        public bool StopJob(int jobId)
        {
            var logger = LogManager.GetLogger("nlogger");
            try
            {
                if (jobId == 0)
                    return false;
                var queue = ProcessingQueues.FirstOrDefault(x => x.JobId == jobId);
                if (queue != null && WorkerManager.StopWork(queue))
                {
                    logger.Info("Очередь {0} остановлена, JobID: {1}", queue.Name, queue.JobId);
                    var connectionString = new NpgsqlConnectionStringBuilder()
                    {
                        Host = "192.168.170.215",
                        Port = 5432,
                        UserName = "postgres",
                        Password = "postgres",
                        Database = "websmr"
                    };
                    using (var conn = new NpgsqlConnection(connectionString.ToString()))
                    {
                        conn.Open();
                        var update = string.Format("UPDATE public.jobs SET job_state = {0}, end_date = now(), success = 'f', message = 'Задача прервана пользователем' WHERE id = :id", JobState.End.GetHashCode());
                        using (var comm = new NpgsqlCommand(update, conn))
                        {
                            comm.Parameters.Add(new NpgsqlParameter("id", jobId));
                            comm.ExecuteNonQuery();
                        }
                    }
                    queue.Stop();
                }
                return false;
            }
            catch (Exception ex)
            {
                logger.ErrorException("Ошибка получения JobId для остановки.", ex);
                return false;   
            }
        }

        /// <summary>
        /// Перезапуск задачи
        /// </summary>
        /// <param name="jobId">идентификатор задачи</param>
        /// <returns></returns>
        public bool RestartJob(int jobId)
        {
            var logger = LogManager.GetLogger("nlogger");
            if (jobId == 0)
                return false;
            try
            {
                var queue = ProcessingQueues.FirstOrDefault(x => x.JobId == jobId);
                if (queue != null && WorkerManager.StopWork(queue))
                {
                    logger.Info("Очередь {0} остановлена для перезапуска, JobID: {1}", queue.Name, queue.JobId);
                    var connectionString = new NpgsqlConnectionStringBuilder()
                    {
                        Host = "192.168.170.215",
                        Port = 5432,
                        UserName = "postgres",
                        Password = "postgres",
                        Database = "websmr"
                    };
                    using (var conn = new NpgsqlConnection(connectionString.ToString()))
                    {
                        conn.Open();
                        //var update = string.Format("UPDATE public.jobs SET job_state = {0}, start_date = now() WHERE id = (:ids)", JobState.Proccess.GetHashCode());
                        //using (var comm = new NpgsqlCommand(update, conn))
                        //{
                        //    comm.Parameters.Add(new NpgsqlParameter("ids", jobId));
                        //    comm.ExecuteNonQuery();
                        //}
                        var select = string.Format("SELECT data, job_code, job_name FROM public.jobs WHERE id = {0}", jobId);
                        using (var comm = new NpgsqlCommand(select, conn))
                        using (var reader = comm.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                logger.Info("Не найдена задача с JobID = {0}", jobId);
                                return false;
                            }
                            var job = new JobArguments()
                            {
                                JobId = jobId,
                                Code = reader["job_code"] != DBNull.Value ? Convert.ToString(reader["job_code"]) : string.Empty,
                                Name = reader["job_name"] != DBNull.Value ? Convert.ToString(reader["job_name"]) : string.Empty
                            };
                            var prms = JsonConvert.DeserializeObject<Dictionary<string, object>>(Convert.ToString(reader["data"]));
                            foreach (var prm in prms)
                            {
                                job.Parameters.Add(prm.Key, prm.Value.ToString());
                            }
                            queue.Start(job.JobId);
                            WorkerManager.RunWork(job, queue);
                            queue.Stop();
                        }
                    }
                    queue.Stop();
                }
                return false;
            }
            catch (Exception ex)
            {
                logger.ErrorException("Ошибка получения JobId для остановки.", ex);
                return false;
            }
        }

        /// <summary>
        /// Обновление heartbeat запущенных задач
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateStates(object sender, DoWorkEventArgs e)
        {
            //todo Добавить "пульс" в конфиг
            var pulse = 10;
            while (true)
            {
                System.Threading.Thread.Sleep(new TimeSpan(0, 0, pulse));
                var IN = string.Join(",",
                    ProcessingQueues.Where(x => x.State == QueueState.InProgress).Select(x => x.JobId));
                if (string.IsNullOrWhiteSpace(IN)) continue;

                var connectionString = new NpgsqlConnectionStringBuilder()
                {
                    Host = "192.168.170.215",
                    Port = 5432,
                    UserName = "postgres",
                    Password = "postgres",
                    Database = "websmr"
                };

                try
                {
                    using (var conn = new NpgsqlConnection(connectionString.ToString()))
                    {
                        conn.Open();
                        var update = string.Format("UPDATE public.jobs SET heart_beat = NOW() WHERE id IN ({0})", IN);
                        using (var comm = new NpgsqlCommand(update, conn))
                        {
                            comm.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    var logger = LogManager.GetLogger("nlogger");
                    logger.ErrorException("Ошибка обновления поля heart_beat", ex);
                }
            }
        }
    }
}
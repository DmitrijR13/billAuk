using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bars.QueueListener.Domain;
using NLog;
using Npgsql;
using NpgsqlTypes;

namespace Bars.QueueListener
{
    /// <summary>
    /// Класс очередей
    /// </summary>
    public class Queue
    {
        /// <summary>
        /// Состояние
        /// </summary>
        public QueueState State { get; set; }

        /// <summary>
        /// Номер задачи в таблице jobs
        /// </summary>
        public int JobId { get; set; }

        /// <summary>
        /// Название
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Домен
        /// </summary>
        public IAppDomainProxy Domain { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="name">Имя очереди</param>
        public Queue(string name)
        {
            Name = name;
            State = QueueState.Listen;
            JobId = 0;
        }

        /// <summary>
        /// Запуск задачи
        /// </summary>
        /// <param name="jobId"></param>
        public void Start(int jobId)
        {
            JobId = jobId;
            State = QueueState.InProgress;
        }

        /// <summary>
        /// Окончинае задачи
        /// </summary>
        public void Stop()
        {
            State = QueueState.Listen;
        }
    }
}

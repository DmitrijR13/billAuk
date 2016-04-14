namespace Bars.RabbitMq
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    using RabbitMQ.Client;

    /// <summary>Публикатор сообщений в очередь</summary>
    public class Producer : IDisposable
    {
        /// <summary>Конструктор публикатора</summary>
        /// <param name="hostName">Имя хоста очереди (Пример: "localhost")</param>
        /// <param name="queueName">Имя очереди, в которую будет публиковаться сообщения (Пример: "reports")</param>
        public Producer(string hostName, string queueName)
        {
            QueueName = queueName;
            var connectionFactory = new ConnectionFactory { HostName = hostName };
            Connection = connectionFactory.CreateConnection();
            Model = Connection.CreateModel();
            Model.QueueDeclare(QueueName, true, false, false, null);
        }

        protected IModel Model { get; set; }

        protected IConnection Connection { get; set; }

        protected string QueueName { get; set; }

        public void Dispose()
        {
            if (Connection != null)
            {
                Connection.Close();
            }

            if (Model != null)
            {
                Model.Abort();
            }
        }

        /// <summary>Метод публикации сообщения в очередь. Пока закрыт (По причине строгого формата обмена сообщениями)</summary>
        /// <param name="message">Сообщение</param>
        private void SendMessage(byte[] message)
        {
            IBasicProperties basicProperties = Model.CreateBasicProperties();
            basicProperties.SetPersistent(true);
            Model.BasicPublish(string.Empty, QueueName, basicProperties, message);
        }

        /// <summary>Метод публикации сообщения в очередь. Пока закрыт (По причине строгого формата обмена сообщениями)</summary>
        /// <typeparam name="T">Тип сообщения</typeparam>
        /// <param name="message">Сообщение</param>
        public void SendMessage<T>(T message)
        {
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, message);
                SendMessage(ms.ToArray());
            }
        }
    }
}
using System;
using System.IO;

namespace Bars.RabbitMq
{
    using RabbitMQ.Client;
    using RabbitMQ.Client.Exceptions;
    using STCLINE.KP50.Global;

    /// <summary>Подписчик</summary>
    public class Consumer
    {
        /// <summary>Конструтктор подписчика</summary>
        /// <param name="hostName">Имя хоста</param>
        /// <param name="queueName">Имя очереди</param>
        public Consumer(string hostName, string queueName)
        {
            QueueName = queueName;
            var connectionFactory = new ConnectionFactory { HostName = hostName };
            Connection = connectionFactory.CreateConnection();
            Model = Connection.CreateModel();
            Model.BasicQos(0, 1, false);
            
            Model.QueueDeclare(QueueName, true, false, false, null);
        }

        /// <summary>used to pass messages back to for processing</summary>
        /// <param name="message">Сообщение</param>
        /// <param name="queue">Очередь</param>
        public delegate void OnReceiveMessage(byte[] message, string queue);

        /// <summary>internal delegate to run the consuming queue on a seperate thread</summary>
        private delegate void ConsumeDelegate();

        /// <summary>Событие получения сообщения</summary>
        public event OnReceiveMessage OnMessageReceived;

        protected IModel Model { get; set; }

        protected IConnection Connection { get; set; }

        protected string QueueName { get; set; }

        protected bool IsConsuming { get; set; }

        /// <summary>
        /// Запуск примеки сообщений
        /// </summary>
        public void StartConsuming()
        {
            IsConsuming = true;
            var consumeDelegate = new ConsumeDelegate(Consume);
            consumeDelegate.BeginInvoke(null, null);
        }

        /// <summary>Метод извлечения сообщений из очереди</summary>
        public void Consume()
        {
            var consumer = new QueueingBasicConsumer(Model);
            var consumerTag = Model.BasicConsume(QueueName, false, consumer);
            while (IsConsuming)
            {
                try
                {
                    var e = consumer.Queue.Dequeue();
                    byte[] body = e.Body;

                    // ... process the message
                    OnMessageReceived(body, QueueName);
                    Model.BasicAck(e.DeliveryTag, false);
                }
                catch (OperationInterruptedException exc)
                {
                    MonitorLog.WriteLog("Ошибка в процедуре Consume: " + exc.ToString(), MonitorLog.typelog.Error, 20,
                        201, true);
                    Model.BasicCancel(consumerTag);
                    Dispose();
                    break;
                }
                catch (EndOfStreamException)
                {
                    Dispose();
                    break;
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteException("Критическая ошибка в Consume: ", ex);
                    Model.BasicCancel(consumerTag);
                    Dispose();
                    break;
                }
            }
        }

        public void Dispose()
        {
            try
            {
                IsConsuming = false;
                if (Connection != null)
                {
                    Connection.Close();
                }

                if (Model != null)
                {
                    Model.Abort();
                }
            }
            catch{}
        }
    }
}
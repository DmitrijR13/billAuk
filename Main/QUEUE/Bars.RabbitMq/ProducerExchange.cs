using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Bars.QueueCore;
using RabbitMQ.Client;

namespace Bars.RabbitMq
{
    public class ProducerExchange : ConnectToRabbitMq
    {
        public ProducerExchange(string server, string exchange, string exchangeType)
            : base(server, exchange, exchangeType)
        {
        }

        public void SendMessage(byte[] message)
        {
            IBasicProperties basicProperties = Model.CreateBasicProperties();
            basicProperties.SetPersistent(true);
            Model.BasicPublish(ExchangeName, "", basicProperties, message);
        }

        /// <summary>Метод публикации сообщения в очередь</summary>
        /// <param name="message">Сообщение</param>
        public void SendMessage(JobSystemArguments message)
        {
            SendMessage<JobSystemArguments>(message);
        }

        /// <summary>Метод публикации сообщения в очередь. Пока закрыт (По причине строгого формата обмена сообщениями)</summary>
        /// <typeparam name="T">Тип сообщения</typeparam>
        /// <param name="message">Сообщение</param>
        private void SendMessage<T>(T message)
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

using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Bars.RabbitMq
{
    /// <summary>
    /// Источник http://simonwdixon.wordpress.com/2011/05/19/getting-started-with-rabbitmq-in-net-–-part-3/
    /// </summary>
    public abstract class ConnectToRabbitMq : IDisposable
    {
        protected IModel Model { get; set; }
        protected IConnection Connection { get; set; }
        public string Server { get; set; }
        public string ExchangeName { get; set; }
        public string ExchangeTypeName { get; set; }

        const bool Durable = true;

        protected ConnectToRabbitMq(string server, string exchange, string exchangeType)
        {
            Server = server;
            ExchangeName = exchange;
            ExchangeTypeName = exchangeType;
        }
        //Create the connection, Model and Exchange(if one is required)
        public virtual bool ConnectToRabbitMQ()
        {
            try
            {
                var connectionFactory = new ConnectionFactory {HostName = Server};
                Connection = connectionFactory.CreateConnection();
                Model = Connection.CreateModel();
                
                if (!String.IsNullOrEmpty(ExchangeName))
                    Model.ExchangeDeclare(ExchangeName, ExchangeTypeName, Durable);
                return true;
            }
            catch (BrokerUnreachableException)
            {
                return false;
            }
        }

        public void Dispose()
        { 
            if (Connection != null)
                Connection.Close();
            if (Model != null)
                Model.Abort();
        }
    }
}

using System;
using System.IO;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.MessagePatterns;
using STCLINE.KP50.Global;

namespace Bars.RabbitMq
{
    public class ConsumerExchange : ConnectToRabbitMq
    {
        const bool AutoAck = false;
        protected bool IsConsuming;
        protected string QueueName;
 
        // used to pass messages back to UI for processing
        public delegate void onReceiveMessage(byte[] message);
        public event onReceiveMessage onMessageReceived;

        public ConsumerExchange(string server, string exchange, string exchangeType)
            : base(server, exchange, exchangeType)
        {
        }
 
        //internal delegate to run the consuming queue on a seperate thread
        private delegate void ConsumeDelegate();
 
        public void StartConsuming()
        {
                Model.BasicQos(0, 1, false);
                QueueName = Model.QueueDeclare();
                Model.QueueBind(QueueName, ExchangeName, "");
                IsConsuming = true;
                var c = new ConsumeDelegate(Consume);
                c.BeginInvoke(null, null);
        }
 
        protected Subscription mSubscription { get; set; }
 
        private void Consume()
        {
            //create a subscription
            mSubscription = new Subscription(Model, QueueName, AutoAck);
 
            while (IsConsuming)
            {
                try
                {
                    var e = mSubscription.Next();
                    var body = e.Body;
                    onMessageReceived(body);
                    mSubscription.Ack(e);
                }
                catch (OperationInterruptedException exc)
                {
                    MonitorLog.WriteLog("Ошибка в процедуре Consume (Exchange): " + exc.ToString(), MonitorLog.typelog.Error, 20,
                        201, true);
                    mSubscription.Model.BasicCancel(mSubscription.ConsumerTag);
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
                    MonitorLog.WriteException("Критическая ошибка в Consume (Exchange): ", ex);
                    mSubscription.Model.BasicCancel(mSubscription.ConsumerTag);
                    Dispose();
                    break;
                }
            }
        }
 
        public new void Dispose()
        {
            IsConsuming = false;
            base.Dispose();
        }
    }
}

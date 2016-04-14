using System;
using System.Windows.Forms;
using Bars.QueueCore;
using Bars.RabbitMq;

namespace RabbitTest
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;

    public partial class Form1 : Form
    {
        delegate void SetTextCallback(string text);

        private Consumer Consumer { get; set; }

        private readonly object _lockObject = new object();

        public Form1()
        {
            InitializeComponent();
            this.Closed += Form1_Closed;
        }

        void Form1_Closed(object sender, EventArgs e)
        {
            if (Consumer != null)
            {
                Consumer.Dispose();
                Consumer = null;
            }
        }

        private void listenerBt_Click(object sender, EventArgs e)
        {
            resultTb.AppendText("Попытка подключения подождите...\n");
            try
            {
                var consumer = new Consumer(hostTb.Text, queueTb.Text);
                consumer.StartConsuming();

                consumer.OnMessageReceived += Consumer_OnMessageReceived;

                resultTb.AppendText(string.Format("Старт прослушки очереди:{0} на хосте:{1}\n", hostTb.Text, queueTb.Text));
            }
            catch (Exception ex)
            {
                resultTb.AppendText(ex.ToString());
                resultTb.AppendText("\n");
            }
        }

        void Consumer_OnMessageReceived(byte[] message, string queue)
        {
            using (var memoryStream = new MemoryStream(message))
            {
                var deserializer = new BinaryFormatter();
                var result = (JobSystemArguments)deserializer.Deserialize(memoryStream);

                SetText(string.Format("Получено сообщение: {0}\n", result));
            }
        }

        private void SetText(string text)
        {
            if (this.resultTb.InvokeRequired)
            {
                var d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.resultTb.AppendText(text);
                this.resultTb.AppendText("\n");
            }
        }

        private void sendBtn_Click(object sender, EventArgs e)
        {
            try
            {
                using (var producer = new Producer(hostTb.Text, queueTb.Text))
                {
                    producer.SendMessage(messageTb.Text);
                }
                resultTb.AppendText(string.Format("Отправлено сообщение: {0}\n", messageTb.Text));
            }
            catch (Exception exc)
            {
                resultTb.AppendText(exc.ToString());
                resultTb.AppendText("\n");
                throw;
            }
            
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            resultTb.Text = string.Empty;
        }
    }
}

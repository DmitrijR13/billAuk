using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_SmsMessage
    {
        /// <summary>
        /// получить смс-сообщения
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="oper">Принимает значения: SrvGet, SrvFind</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        List<SmsMessage> GetSmsMessage(SmsMessage finder, enSrvOper oper, out Returns ret);

        /// <summary>
        /// получить состояния сообщений
        /// </summary>
        [OperationContract]
        List<SmsMessageStatus> GetSmsMessageStatus(out Returns ret);

        /// <summary>
        /// получить получателей
        /// </summary>
        [OperationContract]
        List<SmsReceiver> GetSmsReceiver(out Returns ret);

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        [OperationContract]
        Returns SendSmsMessage(SmsMessage finder, List<SmsReceiver> list);

        /// <summary>
        /// Сохранить получателя
        /// </summary>
        [OperationContract]
        Returns SaveSmsReceiver(SmsReceiver finder);

        /// <summary>
        /// Удалить получателя
        /// </summary>
        [OperationContract]
        Returns DeleteSmsReceiver(SmsReceiver finder);

        /// <summary>
        /// Переотправить сообщения
        /// </summary>
        [OperationContract]       
        Returns ResendSmsMessage(SmsMessage finder, List<SmsMessage> list);
        
        /// <summary>
        /// Удалить сообщения
        /// </summary>
        [OperationContract]
        Returns DeleteSmsMessage(List<SmsMessage> list);
    }

    public interface ISmsMessageRemoteObject : I_SmsMessage, IDisposable { }

    [DataContract]
    public class SmsMessageStatus : Finder
    {
        // код состояния сообщения
        [DataMember]
        public int message_status_id { get; set; }
        // состояние сообщения
        [DataMember]
        public string message_status { get; set; }

        public SmsMessageStatus()
            : base()
        {
            message_status_id = 0;
            message_status = "";
        }
    }

    [DataContract]
    public class SmsReceiver : Finder
    {
        [DataMember]
        public string num { get; set; }
        // код получателя
        [DataMember]
        public int receiver_id { get; set; }
        // получатель
        [DataMember]
        public string receiver { get; set; }
        // мобильный телефон получателя
        [DataMember]
        public string mobile_phone { get; set; }
        // мобильный телефон получателя
        [DataMember]
        public string mobile_phone_show { get; set; }
        // должность
        [DataMember]
        public string post { get; set; }
        public SmsReceiver()
            : base()
        {
            num = "";
            receiver_id = 0;
            receiver = "";
            mobile_phone = "";
            mobile_phone_show = "";
            post = "";
        }
    }

    [DataContract]
    public class SmsMessage : SmsReceiver
    {
        [DataMember]
        public int message_id { get; set; }
        // тип сообщения: сообщение об аварии, сообщение об устраненении аварии 
        [DataMember]
        public Int16 supg_message_type_id { get; set; }
        // тип сообщения: сообщение об аварии, сообщение об устраненении аварии 
        [DataMember]
        public string message_type { get; set; }
        // текст сообщения 
        [DataMember]
        public string sms_text { get; set; }
        // дата создания
        [DataMember]
        public string created_on { get; set; }
        [DataMember]
        public string created_on_to { get; set; }
        // кто создал сообщение
        [DataMember]
        public string creator { get; set; }
        // дата отправки
        [DataMember]
        public string sended_on { get; set; }
        [DataMember]
        public string sended_on_to { get; set; }
        // код отправителя
        [DataMember]
        public int sended_by { get; set; }
        // отправитель
        [DataMember]
        public string sender { get; set; }
        // отправленные сообщения
        [DataMember]
        public Int16 is_sended { get; set; }
        [DataMember]
        public int sms_id { get; set; }
        // код состояния сообщения
        [DataMember]
        public int message_status_id { get; set; }
        // состояние сообщения
        [DataMember]
        public string message_status { get; set; }
        // комментарий
        [DataMember]
        public string status_message { get; set; }

        public SmsMessage()
            : base()
        {
            message_id = 0;
            supg_message_type_id = 0;
            message_type = "";
            sms_text = "";
            created_on = "";
            created_on_to = "";
            creator = "";
            sended_on = "";
            sended_on_to = ""; 
            sended_by = 0;
            sender = "";
            is_sended = 0;
            sms_id = 0;
            message_status_id = 0;
            message_status = "";
            status_message = "";
        }

       
    }
}

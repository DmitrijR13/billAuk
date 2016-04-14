using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Bars.Interfaces
{
    [ServiceContract(Namespace = Constants.Linespace, Name = "ICounter")]
    public interface ICounter
    {
        /// <summary>
        /// Получить список приборов учета для заданного лицевого счета
        /// </summary>
        /// <param name="adresID">Идентификатор лицевого счета</param>
        /// <param name="ret">Результат работы сервиса</param>
        /// <returns>Список приборов учета</returns>
        [OperationContract(Name = "GetCounters")]
        CounterResult GetCounters(AdresID adresID);
    }

    [DataContract(Namespace = Constants.Linespace, Name = "CounterResult")]
    public class CounterResult : ServiceResult
    {
        [DataMember(Name = "counters")]
        public Counter[] counters { get; set; }

        public CounterResult()
        {
            retcode = Utils.InitReturns();
            counters = new Counter[0];
        }
    }

    [DataContract(Namespace = Constants.Linespace, Name = "Counter")]
    public class Counter : AdresID
    {
        /// <summary>
        /// Уникальный код услуги
        /// </summary>
        public int serviceID;
        /// <summary>
        /// Дата поверки
        /// </summary>
        public string verificationDate;
        /// <summary>
        /// Дата следующей поверки
        /// </summary>
        public string nextVerificationDate;
        /// <summary>
        /// Дата последней модификации данных
        /// </summary>
        public string changedOn;
        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string changedBy;
        /// <summary>
        /// Комментарий
        /// </summary>
        public string note;
        /// <summary>
        /// Дата закрытия
        /// </summary>
        public string closeDate;
        
        /// <summary>
        /// Уникальный код прибора учета (ПУ)
        /// </summary>
        [DataMember(Name = "counterID", Order = 0)]
        public Int64 counterID { get; set; }
        /// <summary>
        /// Заводской номер ПУ
        /// </summary>
        [DataMember(Name = "worksNumber", Order = 1)]
        public string worksNumber { get; set; }
        /// <summary>
        /// Тип ПУ
        /// </summary>
        [DataMember(Name = "typeName", Order = 2)]
        public string typeName { get; set; }
        /// <summary>
        /// Разрядность ПУ
        /// </summary>
        [DataMember(Name = "dimension", Order = 3)]
        public int dimension { get; set; }
        /// <summary>
        /// Название услуги
        /// </summary>
        [DataMember(Name = "serviceName", Order = 4)]
        public string serviceName { get; set; }
        /// <summary>
        /// Масштабный множитель
        /// </summary>
        [DataMember(Name = "scaleMultiplier", Order = 5)]
        public int scaleMultiplier { get; set; }
        /// <summary>
        /// Показания
        /// </summary>
        [DataMember(Name = "values", Order = 6)]
        public CounterVal[] values { get; set; }

        public Counter()
            : base()
        { 
            serviceID = 0;
            verificationDate = "";
            nextVerificationDate = "";
            changedOn = "";
            changedBy = "";
            note = "";
            closeDate = "";

            counterID = 0;
            worksNumber = "";
            typeName = "";
            dimension = 0;
            serviceName = "";
            scaleMultiplier = 0;
            values = new CounterVal[0];
        }
    }

    /// <summary>
    /// Класс для описания показаний приборов учета
    /// </summary>
    [DataContract(Namespace = Constants.Linespace, Name = "CounterVal")]
    public class CounterVal
    {
        /// <summary>
        /// Показание
        /// </summary>
        [DataMember(Name = "val", Order = 0)]
        public decimal val { get; set; }
        /// <summary>
        /// Дата снятия показания
        /// </summary>
        [DataMember(Name = "valDate", Order = 1)]
        public string valDate { get; set; }

        public CounterVal()
        {
            val = 0;
            valDate = "";
        }
    }
}

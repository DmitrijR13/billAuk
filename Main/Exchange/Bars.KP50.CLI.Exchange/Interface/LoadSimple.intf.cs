using System;
using STCLINE.KP50.Global;
using System.Runtime.Serialization;

namespace STCLINE.KP50.Interfaces
{

    [DataContract]
    public class LoadSimpleTable
    {
        [DataMember]
        /// <summary>Уникальный идентификатор</summary>
        public int NzpLoad { get; set; }


        [DataMember]
        /// <summary>Тип загрузки</summary>
        public int NzpTypeLoad { get; set; }

        [DataMember]
        /// <summary>Идентификатор из таблицы "Мои файлы"</summary>
        public int NzpExc { get; set; }

        [DataMember]
        /// <summary>Название организацие источника данных</summary>
        public string SourceOrg { get; set; }

        [DataMember]
        /// <summary>Фио осуществивщего выгрузку</summary>
        public string UserSourceOrg { get; set; }

        [DataMember]
        /// <summary>Имя файла, которые загружал пользователь</summary>
        public string FileName { get; set; }

        [DataMember]
        /// <summary>Процент прогресса загрузки файла</summary>
        public double Percent { get; set; }

        [DataMember]
        /// <summary>Кем осуществляется загрузка</summary>
        public int CreatedBy { get; set; }

        [DataMember]
        /// <summary>Кем создано задание</summary>
        public DateTime CreatedOn { get; set; }

        [DataMember]
        /// <summary>Начало выгрузки</summary>
        public DateTime StartLoad { get; set; }

        [DataMember]
        /// <summary>Начало выгрузки</summary>
        public DateTime FinishLoad { get; set; }

        [DataMember]
        /// <summary>Окончание разборки файла</summary>
        public DateTime StartDisassemble { get; set; }

        [DataMember]
        /// <summary>Окончание разборки файла</summary>
        public DateTime FinishDisassemble { get; set; }

        [DataMember]
        /// <summary>Статус загрузки</summary>
        public ExcelUtility.Statuses StatusLoad { get; set; }

        [DataMember]
        /// <summary>Статус разборки</summary>
        public ExcelUtility.Statuses StatusDisassemble { get; set; }

    }

}

using STCLINE.KP50.Interfaces;
using System;
using System.Runtime.Serialization;

namespace STCLINE.KP50.Global
{
    [Serializable]
    [DataContract]
    public class Deal : Ls
    {
        /// <summary>Ключ</summary>
        [DataMember]
        public int nzp_deal { get; set; }

        ///// <summary>ссылка на ЛС</summary>
        //[DataMember(Name = "nzp_kvar", Order = 20)]
        //public int nzp_kvar { get; set; }

        /// <summary>кол-во детей до 18 лет</summary>
        [DataMember]
        public int children_count { get; set; }

        ///// <summary>Кто добавил</summary>
        //[DataMember(Name = "nzp_user", Order = 40)]
        //public int nzp_user { get; set; }
        [DataMember]
        /// <summary>Ответственный</summary> 
        public string responsible_name { get; set; }

        /// <summary>дата фиксации долга</summary> 
        [DataMember]
        public DateTime debt_fix_date { get; set; }

        /// <summary>сумма задолженности</summary>
        [DataMember]
        public decimal debt_money { get; set; }

        /// <summary>мдентификатор статуса дела</summary>
        [DataMember]
        public int nzp_deal_status { get; set; }

        /// <summary>Название статуса дела</summary>
        [DataMember]
        public string status { get; set; }

        /// <summary>Примечание</summary>
        [DataMember]
        public string comment { get; set; }

        ///// <summary>код УК</summary>
        //[DataMember(Name = "nzp_area", Order = 100)]
        //public int nzp_area { get; set; }

        ///// <summary>ФИО</summary>
        //[DataMember(Name = "fio", Order = 110)]
        //public string fio { get; set; }

        ///// <summary>Адрес</summary>
        //[DataMember(Name = "adr", Order = 120)]
        //public string adr { get; set; }

        /// <summary>Приватизированно 1-да, 0-нет</summary>
        [DataMember]
        public string is_priv { get; set; }

        /// <summary>Дата рождения</summary>
        [DataMember]
        public DateTime dat_rog { get; set; }

        /// <summary>Вид документа</summary>
        [DataMember]
        public int dok { get; set; }

        /// <summary>Серия документа</summary>
        [DataMember]
        public string serij { get; set; }

        /// <summary>Номер документа</summary>
        [DataMember]
        public string nomer { get; set; }

        /// <summary>Место выдачи</summary>
        [DataMember]
        public string vid_mes { get; set; }

        /// <summary>Дата выдачи</summary>
        [DataMember]
        public DateTime vid_dat { get; set; }

        [DataMember]
        public int nzp_group { set; get; }

        [DataMember]
        public int nzp_supp { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }



        public Deal()
        {
            nzp_deal = 0;
            nzp_kvar = 0;
            children_count = 0;
            nzp_user = 0;
            responsible_name = "";
            debt_fix_date = DateTime.MinValue;
            debt_money = 0;
            nzp_deal_status = 0;
            comment = "";
            nzp_serv = 0;
            nzp_supp = 0;

        }
        //получение имени статуса по коду
        public string getNameDealStatus(int nzp_status)
        {
            switch (nzp_status)
            {
                case (int)EnumDealStatuses.Close: return "Закрыто";
                case (int)EnumDealStatuses.Registered: return "Зарегистрировано";
                case (int)EnumDealStatuses.Reminder: return "Напоминание выдано";
                case (int)EnumDealStatuses.Notice: return "Уведомление выдано";
                case (int)EnumDealStatuses.Warning: return "Предупреждение выдано";
                case (int)EnumDealStatuses.AgreementSigned: return "Соглашение подписано";
                case (int)EnumDealStatuses.AgreementViolated: return "Соглашение нарушено";
                case (int)EnumDealStatuses.OrderFormed: return "Судебный приказ сформирован";
                case (int)EnumDealStatuses.LawsuitSubmitted: return "Иск подан в суд";
                default: return "Статус отсутствует";
            }
        }
    }
}

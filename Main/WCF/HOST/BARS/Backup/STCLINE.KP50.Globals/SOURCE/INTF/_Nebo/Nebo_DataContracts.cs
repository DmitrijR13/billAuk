using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Interfaces
{
    /// <summary>
    /// Справочник выгруженных реестров для веб-сервиса Небо
    /// </summary>
    [DataContract]
    public class NeboReestr //данные для реестра Небо
    {
        public NeboReestr()
            : base()
        {
            nzp_nebo_reestr =0;
            type_reestr = 0;
            dat_charge = new DateTime();
            dat_oper = new DateTime();
            is_prepare = 0;
            dat_prepare = new DateTime();
            is_notify = 0;
            dat_notify = new DateTime();
            dat_nebocall = new DateTime();
        }

        [DataMember]
        public int nzp_nebo_reestr { get; set; }
        [DataMember]
        public int type_reestr { get; set; }
        [DataMember]
        public DateTime dat_charge { get; set; }
        [DataMember]
        public DateTime dat_oper { get; set; }
        [DataMember]
        public int is_prepare { get; set; }
        [DataMember]
        public DateTime dat_prepare { get; set; }
        [DataMember]
        public int is_notify { get; set; }
        [DataMember]
        public DateTime dat_notify { get; set; }
        [DataMember]
        public DateTime dat_nebocall { get; set; }

        //число записей для данного реестра
        [DataMember]
        public int count_rows { get; set; }
        //число страниц для данного реестра
        [DataMember]
        public int count_pages { get; set; }
        //контрольная сумма по sum_insaldo
        [DataMember]
        public decimal kontr_sum_insaldo { get; set; }
        //контрольная сумма по sum_money
        [DataMember]
        public decimal kontr_sum_money { get; set; }

     
        /// <summary>
        /// Типы реестра 
        /// </summary>
        public enum NeboReestrTypes
        {
            /// <summary>
            /// Тип не задан
            /// </summary>
            None = 0,

            /// <summary>
            /// Сальдо
            /// </summary>
            Saldo = 1,

            /// <summary>
            /// Поступления
            /// </summary>
            Income = 2
        }


        /// <summary>
        /// Статусы реестра
        /// </summary>
        public enum NeboReestrStatuses
        {
            /// <summary>
            /// к подготовке
            /// </summary>
            OnStart = 0,

            /// <summary>
            /// готово - успешно
            /// </summary>
            Success = 1,

            /// <summary>
            /// Не готово - ошибка
            /// </summary>
            Failure = -1,
            
            /// <summary>
            /// В процессе подготовки
            /// </summary>
            InProcess = 2
        }


        /// <summary>
        /// Статусы передачи реестра
        /// </summary>
        public enum NeboReestrNotifyStatuses
        {
            /// <summary>
            /// к уведомлению
            /// </summary>
            OnNotify = 0,

            /// <summary>
            /// готово - успешно
            /// </summary>
            Success = 1,

            /// <summary>
            /// Не готово - ошибка
            /// </summary>
            Failure = -1,

            /// <summary>
            /// В процессе подготовки
            /// </summary>
            InProcess = 2
        }
    }


    //----------------------------------------------------------------------
    [DataContract(Namespace = Constants.Linespace, Name = "SaldoInfoType")]
    public class NeboSaldo //реестр сальдо по домам (кроме арендаторов)
    //----------------------------------------------------------------------
    {
        public NeboSaldo()
            : base()
        {
            nzp_nebo_rsaldo = 0;
            nzp_nebo_reestr = 0;
            dat_charge = new DateTime();
            typek = 0;
            num_ls = 0;
            pkod = 0;
            nzp_dom = 0;
            nzp_serv = 0;
            nzp_area = 0;
            nzp_supp = 0;
            sum_insaldo = 0;
            sum_real = 0;
            reval = 0;
            izm_saldo = 0;
            sum_money = 0;
            sum_charge = 0;
            sum_outsaldo = 0;
            page_number = 0;
        }
        /// <summary> </summary>
        [DataMember(Name = "nzp_nebo_rsaldo", Order = 10)]
        public int nzp_nebo_rsaldo { get; set; }

        /// <summary> </summary>
        [DataMember(Name = " nzp_nebo_reestr", Order = 20)]
        public int nzp_nebo_reestr { get; set; }

        /// <summary> Сальдовый месяц</summary>
        [DataMember(Name = "dat_charge", Order = 30)]
        public DateTime dat_charge { get; set; }

        /// <summary> </summary>
        [DataMember(Name = " typek", Order = 40)]
        public int typek { get; set; }

        /// <summary> Номер лицевого счёта</summary>
        [DataMember(Name = " num_ls", Order = 50)]
        public int num_ls { get; set; }

        /// <summary> </summary>
        [DataMember(Name = " pkod", Order = 60)]
        public decimal pkod { get; set; }

        /// <summary> Код дома</summary>
        [DataMember(Name = " nzp_dom", Order = 70)]
        public int nzp_dom { get; set; }

        /// <summary> Код услуги</summary>
        [DataMember(Name = " nzp_serv", Order = 80)]
        public int nzp_serv { get; set; }

        /// <summary> Код поставщика</summary>
        [DataMember(Name = " nzp_area", Order = 90)]
        public int nzp_area { get; set; }

        /// <summary> </summary>
        [DataMember(Name = " nzp_supp", Order = 100)]
        public int nzp_supp { get; set; }

        /// <summary> Сальдо входящее</summary>
        [DataMember(Name = " sum_insaldo", Order = 110)]
        public decimal sum_insaldo { get; set; }

        /// <summary> </summary>
        [DataMember(Name = " sum_real", Order = 120)]
        public decimal sum_real { get; set; }

        /// <summary> </summary>
        [DataMember(Name = " reval", Order = 130)]
        public decimal reval { get; set; }

        /// <summary> Изменения сальдо</summary>
        [DataMember(Name = " izm_saldo", Order = 140)]
        public decimal izm_saldo { get; set; }

        /// <summary> Оплаты</summary>
        [DataMember(Name = " sum_money", Order = 150)]
        public decimal sum_money { get; set; }

        /// <summary> Начислено к оплате</summary>
        [DataMember(Name = " sum_charge", Order = 160)]
        public decimal sum_charge { get; set; }

        /// <summary> Исходящее сальдо</summary>
        [DataMember(Name = " sum_outsaldo", Order = 170)]
        public decimal sum_outsaldo { get; set; }

        /// <summary> Номер страницы</summary>
        [DataMember(Name = " page_number", Order = 180)]
        public int page_number { get; set; }

        
    }

    //----------------------------------------------------------------------
    [DataContract(Namespace = Constants.Linespace, Name = "SuppInfType")]
    public class NeboSupp //реестр начислений  по поставщикам 
    //----------------------------------------------------------------------
    {
        public NeboSupp()
            : base()
        {
            nzp_nebo_rfnsupp = 0;
            nzp_nebo_reestr = 0;
            dat_uchet = new DateTime();
            typek = 0;
            num_ls = 0;
            pkod = 0;
            nzp_dom = 0;
            nzp_serv = 0;
            nzp_area = 0;
            nzp_supp = 0;
            sum_prih = 0;        
            page_number = 0;
        }
        /// <summary>уникальны номер записи</summary>
        [DataMember(Name = "nzp_nebo_rfnsupp", Order = 10)]
        public int nzp_nebo_rfnsupp { get; set; }

        /// <summary> </summary>
        [DataMember(Name = "nzp_nebo_reestr", Order = 20)]
        public int nzp_nebo_reestr { get; set; }

        /// <summary> Дата учета</summary>
        [DataMember(Name = "dat_uchet", Order = 30)]
        public DateTime dat_uchet { get; set; }

        /// <summary> </summary>
        [DataMember(Name = "typek", Order = 40)]
        public int typek { get; set; }

        /// <summary> Номер лицевого счёта</summary>
        [DataMember(Name = "num_ls", Order = 50)]
        public int num_ls { get; set; }

        /// <summary> Платежный код</summary>
        [DataMember(Name = "pkod", Order = 60)]
        public decimal pkod { get; set; }

        /// <summary> Код дома</summary>
        [DataMember(Name = "nzp_dom", Order = 70)]
        public int nzp_dom { get; set; }

        /// <summary> Код услуги</summary>
        [DataMember(Name = "nzp_serv", Order = 80)]
        public int nzp_serv { get; set; }

        /// <summary> Код УК</summary>
        [DataMember(Name = "nzp_area", Order = 90)]
        public int nzp_area { get; set; }

        /// <summary>Код поставщика </summary>
        [DataMember(Name = "nzp_supp", Order = 100)]
        public int nzp_supp { get; set; }

        /// <summary> </summary>
        [DataMember(Name = "sum_prih", Order = 110)]
        public decimal sum_prih { get; set; }
      
        /// <summary> Номер страницы</summary>
        [DataMember(Name = "page_number", Order = 120)]
        public int page_number { get; set; }


    }
    
}

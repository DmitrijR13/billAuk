using System;
using System.Text;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading;
using Castle.Windsor;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using System.IO;
using Globals.SOURCE.INTF;


namespace STCLINE.KP50.Global
{
    /// <summary>
    /// класс для параметров переноса
    /// </summary>
    [Serializable]
    public class TransferParams : Finder
    {
        /// <summary>
        /// идентификатор пользователя
        /// </summary>
        [DataMember]
        public User user { set; get; }

        /// <summary>
        /// комментарий
        /// </summary>
        [DataMember]
        public string comment { set; get; }

        /// <summary>
        /// дома
        /// </summary>
        [DataMember]
        public List<Dom> houses { set; get; }

        /// <summary>
        /// текущий дом
        /// </summary>
        [DataMember]
        public Dom current_house { set; get; }

        /// <summary>
        /// банк из которого переносят
        /// </summary>
        [DataMember]
        public _Point fPoint;

        /// <summary>
        /// банк в который переносят
        /// </summary>
        [DataMember]
        public _Point tPoint;

        /// <summary>
        /// код файла в excel_utility
        /// </summary>
        public int nzp_exc;

        /// <summary>
        /// Имя сохраненного файла
        /// </summary>
        public string saved_name;

        /// <summary>
        /// Лог ошибок
        /// </summary>
        public string saved_name_log;

        /// <summary>
        /// идентификатор переноса
        /// </summary>
        [DataMember]
        public int transfer_id { get; set; }

    }

    /// <summary>
    /// Приоритет, который придается таблице при переносе
    /// </summary>
    public enum TransferPriority
    {
        Prm17 = 100,                //local_data.prm_17 параметры пу
        Sqadd = 100,                //local_data.sqadd
        Lgots = 100,                //local_data.lgots
        Sobstw = 50,                //local_data.sobstw
        Notice = 50,                //local_data.notice
        NedopKvar = 50,             //local_data.nedop_kvar       
        Family = 50,                //local_data.family
        Bills = 50,                 //local_data.bills
        AliasLs = 50,               //local_data.alias_ls
        CountersGroup = 31,        //local_data.counters_group 
        CountersDomSpis = 30,       //local_data.counters_domspis
        CountersDom = 29,           //local_data.counters_dom
        CountersLink = 28,          //local_data.counters_link
        Counters = 27,              //local_data.counters
        Counters_xx = 26,           //local_charge_XX.counters_xx
        CounterSpis = 25,           //local_data.counters_spis
        PereGilec = 24,             //local_data.pere_gilec
        Perekidka = 23,             //local_charge_XX.perekidka
        ChargeFromSupplier = 22,    //local_charge_XX.from_supplierxx
        ChargeFnSupplier = 21,      //local_charge_XX.fn_supplierxx оплаты
        Calc_gku = 20,              //local_charge_XX.calc_gku_xx
        Gil_xx = 19,                //local_charge_XX.gil_xx
        GilPeriods = 18,            //local_data.gil_periods
        Kart = 17,                  //local_data.kart карточки прибытия\убытия жильцов
        Gilec = 16,                 //local_data.gilec
        Charge = 15,                //local_charge_XX.charge_xx   
        ChargeReval = 14,           //local_charge_XX.reval_xx 
        Prm15 = 13,                 //local_data.prm_15 
        Prm3 = 12,                   //local_data.prm_3 
        Prm1 = 11,                   //local_data.prm_1 
        Tarif = 10,                  //local_data.tarif список услуг лс
        Services = 9,               //central_kernel.services 
        Supplier = 8,               //central_kernel.supplier договора
        Formuls = 7,                //central_kernel.formuls формулы расчета
        Kvar = 6,                   //local_data.kvar
        Prm2 = 5,                   //local_data.prm_2
        Prm4 = 4,                   //local_data.prm_4
        House = 3,                  //local_data.dom 
        Area = 2,                   //local_data.s_area ук
        Geu = 1                     //local_data.s_geu территории
    }
    /// <summary>
    /// Атрибуты переноса
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TransferAttributes : Attribute
    {
        /// <summary>
        /// приоритет, оперделяющий порядок переноса данных в таблице
        /// </summary>
        public TransferPriority Priority { set; get; }
        /// <summary>
        /// наименование переносимой таблицы (необязательно)
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// описание переносимой таблицы
        /// </summary>
        public string Descr { get; set; }
        /// <summary>
        /// видимость таблицы при переносе
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// Банк данных
        /// </summary>
        public string Bank { get; set; }
    }

    public struct TableContains
    {
        public string name { get; set; }
        public string type { get; set; }
    }
}

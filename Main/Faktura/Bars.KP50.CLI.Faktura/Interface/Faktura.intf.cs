using System;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Faktura
    {
        /// <summary>
        /// Возвращает сформированный счет по одному ЛС
        /// </summary>
        /// <param name="finder">Указывается ID счета </param>
        /// <param name="ret">Результат</param>
        /// <returns></returns>
        [OperationContract]
        List<string> GetFaktura(Faktura finder, out Returns ret);

        /// <summary>
        /// Получение списка наименование счетов-квитанций
        /// </summary>
        /// <param name="ret"></param>
        /// <returns>Список видов счетов квитанций в формате код=Наименование</returns>
        [OperationContract]
        List<string> GetListFaktura(out Returns ret);

        [OperationContract]
        Returns GetFakturaWeb(int sessionId);

        [OperationContract]
        List<Charge> GetBillCharge(ChargeFind finder, out Returns ret);

        [OperationContract]
        List<Charge> GetNewBillCharge(ChargeFind finder, out Returns ret);

        [OperationContract]
        int GetDefaultFaktura(Ls finder, out Returns ret);

        [OperationContract]
        string GetCustomTextFaktura(string facturaName, out Returns ret);

        [OperationContract]
        void SaveCustomTextFaktura(string facturaName, string newCustomText, out Returns ret);
    }

    public interface IFakturaRemoteObject : I_Faktura, IDisposable { }

    //----------------------------------------------------------------------
    [DataContract]
    public class Faktura
    //----------------------------------------------------------------------
    {
        public enum Kinds
        {
            None = 0,

            /// <summary>
            /// Ежемесячный платеж ЖСК
            /// </summary>
            MonthlyGSK = 23,

            /// <summary>
            /// Ежемесячный платеж муниуипальный
            /// </summary>
            MonthlyMunicipal = 33,

            /// <summary>
            /// Суммы автоматического перераспределения сальдо оплатами в РЦ
            /// </summary>
            SaldoAutomaticRedistribution = 39,

            /// <summary>
            /// Зачисление средств на счет (без указания месяца, за который оплачивается, не ежемесячный, например, платеж через банкомат)
            /// </summary>
            FreePayment = 41,


            /// <summary>
            /// оплаты УК и поставщиков 
            /// </summary>
            OplatiUKiPost = 49,

            /// <summary>
            /// оплаты по договорам
            /// </summary>
            OplatiDogovor = 50,

            /// <summary>
            /// суммы квитанций по требованию жильца (Казань)
            /// </summary>
            PoTrebovaniuGilca = 55,

            /// <summary>
            /// суммы квитанций с указанием оплаты по статьям жильцом
            /// </summary>
            GilecUkazalSummiPoUslugam = 57,
            kind81 = 81,
            kind83 = 83,
            kind93 = 93,
            kind94 = 94,
            kod40 = 40,
            kod35 = 35
        }
        /// <summary>
        /// Счет-фактуры по старому алгоритму
        /// </summary>
        public enum FakturaTypes
        {
            None = 0,
            /// <summary>
            /// Самара код 100
            /// </summary>
            SamaraFakturacase = 100,
            /// <summary>
            /// Самара долговая код 101
            /// </summary>
            SamaraFakturaDolg = 101,
            /// <summary>
            /// Саха код 102
            /// </summary>
            SahaFaktura = 102,
            /// <summary>
            /// Стандартная код 103
            /// </summary>
            StandartFaktura = 103,
            /// <summary>
            /// Губкин код 104
            /// </summary>
            GubkinFaktura = 104,
            /// <summary>
            /// Капремонт код 105
            /// </summary>
            KapremontFaktura = 105,
            /// <summary>
            /// Астрахань код 106
            /// </summary>
            AstrahanFaktura = 106,
            /// <summary>
            /// Тула ЕПД код 107
            /// </summary>
            TulaFaktura = 107,
            /// <summary>
            /// Калуга код 108
            /// </summary>
            KalugaFaktura = 108,
            /// <summary>
            /// Уютный дом код 109
            /// </summary>
            KznUyutdFaktura = 109,
            /// <summary>
            /// ЕПД по Зеленодольску код 111
            /// </summary>
            ZelFaktura = 111,
            /// <summary>
            /// ЕПД Жигулевск код 112
            /// </summary>
            ZhigulFaktura = 112,
            /// <summary>
            /// Самара оплата по поставщикам код 113
            /// </summary>
            SuppSamaraFaktura = 113,
            /// <summary>
            /// ЕПД для РСО код 114
            /// </summary>
            NorthOsetiaFactura = 114,
            /// <summary>
            /// РСО оплата вперед код 115
            /// </summary>
            NorthOsetiaFacturaAdvance = 115,
            /// <summary>
            /// Платежка по кап.ремонту для Самары код 116
            /// </summary>
            SamaraKapr = 116,
            /// <summary>
            /// Жигулевск капремонт код 117
            /// </summary>
            ZhigulKapr = 117,
            /// <summary>
            /// Безенчук капремонт код 118
            /// </summary>
            BezenchukKapr = 118,
            /// <summary>
            /// Жигулевск капремонт с корректировкой по отоплению код 119
            /// </summary>
            ZhigulKaprOtopl = 119,
            /// <summary>
            /// Безенчук капремонт с корректировкой по отоплению код 120
            /// </summary>
            BezenchukKaprOtopl = 120,
            /// <summary>
            /// Жигулевск отопление код 121
            /// </summary>
            ZhigulOtopl = 121,
            /// <summary>
            /// Тула с мод. ЕПД код 122
            /// </summary>
            TulaFakturaMod = 122,
            /// <summary>
            /// Тула с мод. ЕПД код 123
            /// </summary>
            TulaWithoutBarcode = 123

        }
        /// <summary>
        /// Возвращает признак долговой Счет-Фактуры 
        /// </summary>
        /// <param name="kod">код фактуры</param>
        /// <returns>true/false</returns>
        public static bool DolgFaktura(int kod)
        {
            switch (kod)
            {
                //case 100:
                //    return true;
                //case 101:
                //    return true;
                //case 112:
                //    return true;
                //case 117:
                //    return true;
                case 118:
                    return true;
                default: return false;
            }

        }

        /// <summary>
        /// Возвращает вид счета-фактуры по коду
        /// </summary>
        /// <param name="id">Код вида счета-фактуры</param>
        /// <returns>Вид счета-фактуры</returns>
        public static Kinds GetKindById(short id)
        {
            if (id == (int)Kinds.MonthlyGSK) return Kinds.MonthlyGSK;
            if (id == (int)Kinds.MonthlyMunicipal) return Kinds.MonthlyMunicipal;
            if (id == (int)Kinds.FreePayment) return Kinds.FreePayment;
            if (id == (int)Kinds.GilecUkazalSummiPoUslugam) return Kinds.GilecUkazalSummiPoUslugam;
            if (id == (int)Kinds.kind81) return Kinds.kind81;
            if (id == (int)Kinds.kind83) return Kinds.kind83;
            if (id == (int)Kinds.kind93) return Kinds.kind93;
            if (id == (int)Kinds.kod40) return Kinds.kod40;
            if (id == (int)Kinds.kind94) return Kinds.kind94;
            if (id == (int)Kinds.PoTrebovaniuGilca) return Kinds.PoTrebovaniuGilca;
            if (id == (int)Kinds.OplatiUKiPost) return Kinds.OplatiUKiPost;
            if (id == (int)Kinds.OplatiDogovor) return Kinds.OplatiDogovor;
            if (id == (int)Kinds.SaldoAutomaticRedistribution) return Kinds.SaldoAutomaticRedistribution;
            if (id == (int)Kinds.kod35) return Kinds.kod35;
            return Kinds.None;
        }

        //----------------------------------------------------------------------
        public enum WorkFakturaRegims
        //----------------------------------------------------------------------
        {
            One,
            Group,
            Bank,
            Web
        }

        //----------------------------------------------------------------------
        public enum FakturaFileTypes
        //----------------------------------------------------------------------
        {
            FPX,
            PDF
        }


        [DataMember]
        public RecordMonth YM;

        [DataMember]
        public int month_
        {
            get
            {
                return YM.month_;
            }
            set
            {
                YM.month_ = value;
            }
        }
        [DataMember]
        public int year_
        {
            get
            {
                return YM.year_;
            }
            set
            {
                YM.year_ = value;
            }
        }


        /// <summary> Код счета, по которуму определяется тип алгоритма заполнения и форма счета
        /// </summary>
        [DataMember]
        public int idFaktura { get; set; }


        /// <summary> Код квитанции
        /// </summary>
        [DataMember]
        public Kinds kodSumFaktura { get; set; }

        /// <summary> Режим формирования (enWfrOne - 1 счет, enWfrGroup - выбранный список счетов, enWfrBank - банк данных
        /// </summary>
        [DataMember]
        public WorkFakturaRegims workRegim { get; set; }

        /// <summary>
        /// Тип выходного файла FPX или PDF
        /// </summary>
        [DataMember]
        public FakturaFileTypes resultFileType { get; set; }

        /// <summary> Количество счетов формируемых в пачке
        /// </summary>
        [DataMember]
        public int countListInPack { get; set; }


        /// <summary> Наименование выходного файла
        /// </summary>
        [DataMember]
        public string destFileName { get; set; }

        /// <summary> Печатать с долгом
        /// </summary>
        [DataMember]
        public bool withDolg { get; set; }

        [DataMember]
        public bool withUk { get; set; }

        [DataMember]
        public bool withGeu { get; set; }

        [DataMember]
        public bool withUchastok { get; set; }


        /// <summary> Новая сумма к оплате
        /// </summary>
        [DataMember]
        public decimal newSumOpl { get; set; }


        /// <summary> Кол-во месяцев для оплаты вперед
        /// </summary>
        [DataMember]
        public int AdvancePaymentCountMonth { get; set; }

        /// <summary> Префикс Банка данных, заполнятеся для печати счета по выбранному ЛС
        /// </summary>
        [DataMember]
        public string pref;

        /// <summary> Код квартиры
        /// </summary>
        [DataMember]
        public int nzp_kvar;

        /// <summary> Код дома
        /// </summary>
        [DataMember]
        public int nzp_dom;

        /// <summary> Код пользователя
        /// </summary>
        [DataMember]
        public int nzp_user;

        /// <summary> Код территории
        /// </summary>
        [DataMember]
        public int nzp_area;

        /// <summary> Код жэу
        /// </summary>
        [DataMember]
        public int nzp_geu;

        /// <summary> Примечание
        /// </summary>
        [DataMember]
        public string pm_note;

        /// <summary> Дата формирования
        /// </summary>
        [DataMember]
        public DateTime printDate;

        /// <summary> Печать нулевых ЕПД </summary>
        [DataMember]
        public bool PrintZero;

        /// <summary> Печать закрытых ЛС </summary>
        [DataMember]
        public bool PrintCloseLs;


        public Faktura()
            : base()
        {
            YM.month_ = 0;
            YM.year_ = 0;
            idFaktura = 0;
            workRegim = WorkFakturaRegims.One;
            resultFileType = FakturaFileTypes.FPX;
            countListInPack = 500;
            destFileName = "";
            withDolg = false;
            withUk = withGeu = withUchastok = false;
            newSumOpl = 0;
            kodSumFaktura = Kinds.MonthlyMunicipal;
            AdvancePaymentCountMonth = 0;
            nzp_kvar = -1;
            nzp_dom = -1;
            nzp_user = -1;
            nzp_area = -1;
            nzp_geu = -1;
            pm_note = String.Empty;
            printDate = DateTime.Now;
            PrintCloseLs = true;
            PrintZero = false;
        }


    }
}


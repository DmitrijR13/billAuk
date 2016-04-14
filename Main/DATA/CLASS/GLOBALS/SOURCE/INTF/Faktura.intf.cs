using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.IO;
using STCLINE.KP50.Global;


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
        Returns GetFakturaWeb(int SessionID);
    }




    //----------------------------------------------------------------------
    [DataContract]
    public class Faktura : Ls
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
            kind94 = 94
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
            ZhigulKapr = 117
        }
        /// <summary>
        /// Возвращает признак долговой Счет-Фактуры 
        /// </summary>
        /// <param name="Kod">код фактуры</param>
        /// <returns>true/false</returns>
        public static bool DolgFaktura(int Kod)
        {
            switch (Kod)
            {
                case 100:
                    return true;
                case 101:
                    return true;
                case 112:
                    return true;
                case 117:
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
            else if (id == (int)Kinds.MonthlyMunicipal) return Kinds.MonthlyMunicipal;
            else if (id == (int)Kinds.FreePayment) return Kinds.FreePayment;
            else if (id == (int)Kinds.GilecUkazalSummiPoUslugam) return Kinds.GilecUkazalSummiPoUslugam;
            else if (id == (int)Kinds.kind81) return Kinds.kind81;
            else if (id == (int)Kinds.kind83) return Kinds.kind83;
            else if (id == (int)Kinds.kind93) return Kinds.kind93;
            else if (id == (int)Kinds.kind94) return Kinds.kind94;
            else if (id == (int)Kinds.PoTrebovaniuGilca) return Kinds.PoTrebovaniuGilca;
            else if (id == (int)Kinds.SaldoAutomaticRedistribution) return Kinds.SaldoAutomaticRedistribution;
            else return Kinds.None;
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


        /// <summary> Новая сумма к оплате
        /// </summary>
        [DataMember]
        public decimal newSumOpl { get; set; }


        /// <summary> Кол-во месяцев для оплаты вперед
        /// </summary>
        [DataMember]
        public int AdvancePaymentCountMonth { get; set; }

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
            newSumOpl = 0;
            kodSumFaktura = Kinds.MonthlyMunicipal;
            AdvancePaymentCountMonth = 0;

        }


    }
}


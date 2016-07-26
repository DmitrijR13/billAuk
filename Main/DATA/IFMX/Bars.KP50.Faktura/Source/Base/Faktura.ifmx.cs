
using System.Globalization;
using System.Linq;
using Bars.KP50.Faktura.Source.Base;

namespace Bars.KP50.DB.Faktura
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Interfaces;
    using System.IO;


    /// <summary>
    /// Основание для перерасчета
    /// </summary>
    public struct ServReval
    {
        public int NzpServ;
        public string ServiceName;
        public string Reason;
        public string ReasonPeriod;
        public decimal SumReval;
        public decimal SumGilReval;
        public decimal CReval;
    }

    /// <summary>
    /// Счетчики
    /// </summary>
    public struct Counters
    {
        public int NzpServ;//Код услуги
        public string ServiceName;//Наименование услуги
        public string NumCounters;//Заводской номер счетчика
        public string Place;//Место подключения счетчика в квартире
        public decimal Value;//Показание счетчика
        public DateTime DatUchet;//Дата показания счетчика
        public decimal ValuePred;//Предыдущее показание счетчика
        public DateTime DatUchetPred;//Дата предыдущего показания счетчика
        public int CntStage; //Разрядность счетчика
        public decimal Formula;//Масштабный множитель
        public string DatProv;//Дата поверки счетчика
        public bool IsGkal;//Признак Гигакаллорного счетчика
        public string Measure;//Единица измерения
    }


    public struct FakturaBlockTable
    {
        public bool HasAdrBlock; //Блок адресной информации
        public bool HasRekvizitBlock;//Блок реквизитов
        public bool HasKvarPrmBlock;//Блок квартирных параметров
        public bool HasSummuryBillBlock;//Блок итоговой информации по начислениям
        public bool HasMainChargeGridBlock;//Блок таблицы начислений
        public bool HasRevalReasonBlock;//Блок причин перерасчета
        public bool HasServiceVolumeBlock;//Блок объема по услугам
        public bool HasRTCountersBlock;//Блок счетчиков по РТ, которые не считают в 5.0
        public bool HasRTCountersDoubleBlock;//Блок счетчиков с предыдущими показаниями, которые не считают в 5.0
        public bool HasRTCountersDoubleDomBlock;//Блок домовых счетчиков с предыдущими показаниями, которые не считают в 5.0

        public bool HasCountersBlock;//Блок счетчиков
        public bool HasCountersDoubleBlock;//Блок счетчиков с предыдущими показаниями
        public bool HasCountersDoubleDomBlock;//Блок домовых счетчиков с предыдущими показаниями

        public bool HasOdnBlock;//Блок вычисления ОДН
        public bool HasRdnBlock;//Блок коэффициентов РДН
        public bool HasPerekidkiSamaraBlock;//Блок перекидок для самары
        public bool HasRassrochka;//Блок рассрочки
        public bool HasNormblock;//Блок загрузки нормативов
        public bool HasRemarkblock;//Блок отображения примечаний
        public bool HasGilPeriodsBlock;//Блок подсчета периода временного убытия
        public bool HasDatOplBlock;//Блок вычисления даты последней оплаты
        public bool HasCountersSpisBlock;//Блок загрузки счетчиков
        public bool HasNewCountersBlock;//Блок загрузки счетчиков по новому
        public bool HasNewDoubleCountersBlock;//Блок загрузки счетчиков по новому с предыдущими показаниями
        public bool HasSupplierblock;//Блок отображения поставщикво
        public bool HasSzBlock;//Блок загрузки информации СЗ для Казани
        public bool HasAreaDataBlock;//Блок информации по территории
        public bool HasGeuDataBlock;//Блок информации по ЖЭУ
        public bool HasUpravDomBlock;//Блок Старшего по домам
        public bool HasDomRashodBlock;//Блок расходов по дому по арендаторам и обычным квартирам
        public bool HasArendBlock;//Номер Акта Арендаторов
        public bool HasCalcGil;//Жильцы участвующие в расчете (количество)

        public bool HasNewNachBlock;//Начисления по новому
        public bool HasPrintOrdering;//Запись order_print при формированиии ЕПД
        public bool HasSupplierPkod;//Загружать ли платежный код по поставщикам
        public bool HasBezenchuk;//получение параметров 652 - Для ввода показания ОДПУ ГВС;1104-Бойлер-Объем коммунального ресурса на отопление;1106-Бойлер-Объем коммунального ресурса на ГВС 
    }

    /// <summary>
    /// Расход по услуге
    /// </summary>
    public class ServVolume
    {
        public int NzpServ;
        public string ServiceName;
        public decimal PUVolume;
        public decimal NormaVolume;
        public decimal NormaFullVolume;
        public decimal OdnFlatNormVolume;
        public decimal OdnFlatPuVolume;
        public decimal OdnDomVolume;
        public decimal DomVolume;
        public decimal DomLiftVolume;
        public decimal DomArendatorsVolume;
        public decimal Kf307;
        public decimal AllLsVolume;
        public int IsPu;
        public ServVolume()
        {
            Clear();
        }

        public void Clear()
        {
            NzpServ = 0;
            ServiceName = "";
            PUVolume = 0;
            NormaVolume = 0;
            NormaFullVolume = 0;
            OdnFlatNormVolume = 0;
            OdnFlatPuVolume = 0;
            OdnDomVolume = 0;
            DomVolume = 0;
            DomLiftVolume = 0;
            DomArendatorsVolume = 0;
            IsPu = 0;
            Kf307 = 0;
            AllLsVolume = 0;
        }

    }


    /// <summary>
    /// Набор свойст услуги
    /// </summary>
    public class SumServ : ICloneable
    {
        public string NameServ;
        public string NameSupp;
        public string SuppRekv;
        public int NzpServ;
        public int NzpSupp;
        public int NzpFrm;
        public int IsDevice;
        public int NzpMeasure;
        public int OldMeasure;
        public int Ordering;
        public string Measure;
        public decimal SumInsaldo;
        public decimal SumOutsaldo;
        public decimal Tarif;
        public decimal TarifF;
        public decimal RsumTarif;
        public decimal SumTarif;
        public decimal SumLgota;
        public decimal SumNedop;
        public decimal SumReal;
        public decimal SumSn;
        public decimal Reval;
        public decimal RevalGil;
        public decimal SumPere;
        public decimal RealCharge;
        public decimal SumCharge;
        public decimal SumMoney;
        public decimal CCalc;
        public decimal Norma;
        public decimal CReval;
        public decimal Compensation;
        public bool UnionServ;
        public bool CanAddTarif;
        public bool CanAddVolume;
        public bool IsOdn;
        public int COkaz;
        public decimal OdnDomVolumePu; //текущие показания ПУ ком.услуг. на ОДН
        public SumServ()
        {
            Clear();
        }

        public void Clear()
        {
            NameServ = "";
            NameSupp = "";
            SuppRekv = "";
            Measure = "";
            NzpServ = 0;
            NzpSupp = 0;
            NzpFrm = 0;
            IsDevice = 0;
            NzpMeasure = 0;
            OldMeasure = 0;
            Ordering = 0;
            SumInsaldo = 0;
            SumOutsaldo = 0;
            Tarif = 0;
            TarifF = 0;
            RsumTarif = 0;
            SumTarif = 0;
            SumLgota = 0;
            SumNedop = 0;
            SumReal = 0;
            SumSn = 0;
            Reval = 0;
            RevalGil = 0;
            Norma = 0;
            SumPere = 0;
            RealCharge = 0;
            SumCharge = 0;
            SumMoney = 0;
            CCalc = 0;
            CReval = 0;
            UnionServ = false;
            CanAddTarif = true;
            CanAddVolume = true;
            IsOdn = false;
            COkaz = 0;
            Compensation = 0;
        }

        public void AddSum(SumServ aServ)
        {
            SumInsaldo += aServ.SumInsaldo;
            SumOutsaldo += aServ.SumOutsaldo;
            RsumTarif += aServ.RsumTarif;
            SumTarif += aServ.SumTarif;
            SumLgota += aServ.SumLgota;
            SumNedop += aServ.SumNedop;
            SumReal += aServ.SumReal;
            SumSn += aServ.SumSn;
            Reval += aServ.Reval;
            SumPere += aServ.SumPere;
            RealCharge += aServ.RealCharge;
            SumCharge += aServ.SumCharge;
            SumMoney += aServ.SumMoney;
            Norma += aServ.Norma;
            Compensation += aServ.Compensation;
            NzpSupp = aServ.NzpSupp;

            if (Tarif == 0) IsOdn = aServ.IsOdn;

            if (aServ.IsOdn == false)
                NzpFrm = aServ.NzpFrm;
            if ((NzpServ == 9) & (aServ.NzpServ == 14))
            {
                aServ.CanAddVolume = false;
            }
            if (NzpMeasure == 0)
                NzpMeasure = aServ.NzpMeasure;

            if (OldMeasure == 0)
                OldMeasure = aServ.OldMeasure;

            if ((aServ.Tarif > 0) & (aServ.IsOdn == false))
            {
                NzpMeasure = aServ.NzpMeasure;
                OldMeasure = aServ.OldMeasure;
            }



            if ((NzpMeasure == aServ.NzpMeasure) & (aServ.CanAddVolume))
            {
                CCalc += aServ.CCalc;
                CReval += aServ.CReval;
            }



            if (((CanAddTarif & aServ.CanAddTarif) || (NzpServ == aServ.NzpServ)) &
                (IsOdn == false))
            {
                Tarif += aServ.Tarif;
            }
            else
            {
                Tarif = Math.Max(Tarif, aServ.Tarif);
                // isOdn = aServ.isOdn;
            }

            if (NzpServ != aServ.NzpServ)
            {
                UnionServ = true;
            }
            if ((aServ.Tarif > 0) & (aServ.IsOdn == false)) IsDevice = Math.Max(IsDevice, aServ.IsDevice);

            COkaz = (COkaz == 0) ? aServ.COkaz : (aServ.COkaz < COkaz) ? aServ.COkaz : COkaz;

            if (aServ.IsOdn == false) IsOdn = false;

        }

        public object Clone()
        {
            var newServ = (SumServ)MemberwiseClone();
            return newServ;
        }

    }

    /// <summary>
    /// Базовый класс услуги для вывода в счете на оплату ЖКУ
    /// </summary>
    public class BaseServ : ICloneable, IComparable<BaseServ>
    {
        //Услуга Коммунальная или нет
        public bool KommServ;

        /// <summary>
        /// Основаня сумма по услуге
        /// </summary>
        public SumServ Serv;
        /// <summary>
        /// в т.ч. сумма ОДН по услуге
        /// </summary>
        public SumServ ServOdn;

        public List<SumServ> SlaveServ;

        public BaseServ(bool hasOdn)
        {
            Serv = new SumServ();
            ServOdn = new SumServ();
            Serv.IsOdn = hasOdn;
            KommServ = false;
            SlaveServ = new List<SumServ>();
        }

        public void Clear()
        {
            Serv.Clear();
            ServOdn.Clear();
            SlaveServ.Clear();
        }

        /// <summary>
        /// Добавить к услуге некоторую сумму
        /// </summary>
        /// <param name="aServ"></param>
        public void AddSum(SumServ aServ)
        {
            Serv.AddSum(aServ);
            if (aServ.IsOdn)
            {
                ServOdn.AddSum(aServ);
            }
        }

        public void AddSlave(SumServ aServ)
        {
            bool findServ = false;
            foreach (SumServ ss in SlaveServ)
            {
                if (ss.NzpServ == aServ.NzpServ)
                {
                    ss.AddSum(aServ);
                    findServ = true;
                }
            }
            if (!findServ) SlaveServ.Add(aServ);
        }

        public bool Empty()
        {

            if ((Math.Abs(Serv.Tarif) < 0.001m) &
                (Math.Abs(Serv.RsumTarif) < 0.001m) &
                (Math.Abs(Serv.SumMoney) < 0.001m) &
                (Math.Abs(Serv.SumInsaldo) < 0.001m) &
                (Math.Abs(Serv.SumOutsaldo) < 0.001m) &
                (Math.Abs(Serv.RealCharge) < 0.001m) &
                (Math.Abs(Serv.Reval) < 0.001m)
                )
            {
                return true;
            }

            if ((Math.Abs(Serv.Tarif) > 0.001m) & (Serv.IsOdn) &
                (Math.Abs(Serv.RsumTarif) < 0.001m) &
                (Math.Abs(Serv.SumMoney) < 0.001m) &
                (Math.Abs(Serv.SumInsaldo) < 0.001m) &
                (Math.Abs(Serv.SumOutsaldo) < 0.001m) &
                (Math.Abs(Serv.RealCharge) < 0.001m) &
                (Math.Abs(Serv.Reval) < 0.001m))
            {
                return true;
            }
            return false;
        }

        public void CopyToOdn()
        {
            if (Serv.IsOdn)
            {
                ServOdn.AddSum(Serv);
                ServOdn.CCalc = Serv.CCalc;
                ServOdn.CReval = Serv.CReval;
            }
        }

        public object Clone()
        {
            var newServ = new BaseServ(false)
            {
                Serv = (SumServ) Serv.Clone(),
                ServOdn = (SumServ) ServOdn.Clone(),
                KommServ = KommServ,
                SlaveServ = new List<SumServ>()
            };
            foreach (SumServ ss in SlaveServ)
            {
                newServ.SlaveServ.Add((SumServ)ss.Clone());
            }

            return newServ;
        }

        public int CompareTo(BaseServ other)
        {

            if (other == null) return 1;

            return Serv.Ordering.CompareTo(other.Serv.Ordering);
        }
    }

    /// <summary>
    /// Класс объединенной услуги
    /// </summary>
    public class MasterServ
    {
        /// <summary>
        /// Услуга, к которой присоединяют остальные услуги
        /// </summary>
        public BaseServ MainServ;
        /// <summary>
        /// Список услуг, присоединяемых к основной услуге 
        /// </summary>
        public List<BaseServ> SlaveListServ;

        public MasterServ()
        {
            MainServ = new BaseServ(false);
            SlaveListServ = new List<BaseServ>();
        }

    }

    /// <summary>
    /// Правила объединения услуг 
    /// </summary>
    public class CUnionServ
    {
        public List<MasterServ> MasterList;

        public CUnionServ()
        {
            MasterList = new List<MasterServ>();
        }

        /// <summary>
        /// Получить услугу, в которую объединяют, по коду подчиненной услуги
        /// </summary>
        /// <param name="nzpServ">код подчиненной услуги</param>
        /// <returns>Базовая услуга</returns>
        public BaseServ GetMainServBySlave(int nzpServ)
        {
            return (from t in MasterList where t.SlaveListServ.Any(t1 => t1.Serv.NzpServ == nzpServ) select t.MainServ).FirstOrDefault();
        }

        /// <summary>
        /// Получить мастер услугу(со списком подчиненных услуг) по коду подчиненной услуги
        /// </summary>
        /// <param name="nzpServ">Код подчиненной услуги</param>
        /// <returns>Мастер услуга</returns>
        public MasterServ GetMasterBySlave(int nzpServ)
        {
            return MasterList.FirstOrDefault(t => t.SlaveListServ.Any(t1 => t1.Serv.NzpServ == nzpServ));
        }

        /// <summary>
        /// Добавить услугу в список объединенных услуг
        /// </summary>
        /// <param name="mainServ">Мастер услуга</param>
        /// <param name="slaveServ">Подчиненная услуга</param>
        public void AddServ(BaseServ mainServ, BaseServ slaveServ)
        {
            bool hasMaster = false;
            foreach (MasterServ t in MasterList)
            {
                if (t.MainServ.Serv.NzpServ == mainServ.Serv.NzpServ)
                {
                    bool hasSlave = false;
                    hasMaster = true;

                    foreach (BaseServ t1 in t.SlaveListServ)
                    {
                        if (t1.Serv.NzpServ == slaveServ.Serv.NzpServ)
                        {
                            hasSlave = true;
                        }
                    }
                    if (!hasSlave) //Если подчиненная услуга не была ранее добавлена, то добавляем её
                    {
                        t.SlaveListServ.Add(slaveServ);
                    }
                }
            }
            if (!hasMaster) //Если мастер услуга не найдена, то создает её и добавляет
            {
                var masterServ = new MasterServ {MainServ = mainServ};
                masterServ.SlaveListServ.Add(slaveServ);
                MasterList.Add(masterServ);
            }
        }
    }


    /// <summary>
    /// Описание формулы для самары
    /// </summary>
    public class FormulaDefinition
    {
        /// <summary>
        /// код формулы
        /// </summary>
        public int NzpFrm;

        /// <summary>
        /// код единицы измерения привязанная к формуле
        /// </summary>
        public int FrmMeasure;

        /// <summary>
        /// код единицы измерения привязанная к услуге
        /// </summary>
        public int ServMeasure;

        /// <summary>
        /// Тариф привязанный к формуле
        /// </summary>
        public decimal FrmTarif;

        /// <summary>
        /// Тариф привязанный к услуге
        /// </summary>
        public decimal ServTarif;

        /// <summary>
        /// Единица измерения приведенная 
        /// </summary>
        public string Measure;

        public FormulaDefinition()
        {
            NzpFrm = 0;
            FrmMeasure = 0;
            ServMeasure = 0;
            FrmTarif = 0;
            ServTarif = 0;
            Measure = "";
        }
    }

    /// <summary>
    /// Приведение единиц измерения к одному виду для услуги для Самары
    /// </summary>
    public class ConvertTarif
    {
        public List<FormulaDefinition> ListFormuls;


        public ConvertTarif()
        {
            ListFormuls = new List<FormulaDefinition>();
        }

        public void AddFrm(FormulaDefinition aFrm)
        {
            ListFormuls.Add(aFrm);
        }

        public void ReplaceServiceFrm(ref BaseServ aserv, int nzpFrm)
        {
            foreach (FormulaDefinition t in ListFormuls)
            {
                if (t.NzpFrm != nzpFrm) continue;
                aserv.Serv.NzpMeasure = t.ServMeasure;
                aserv.Serv.Tarif = t.ServTarif;
                aserv.Serv.Measure = t.Measure;
            }
        }
    }


    /// <summary>
    /// Суммы по льготам Социальной защиты для РТ
    /// </summary>
    public class SzSum
    {
        public decimal SumLgota;
        public decimal SumSubs;
        public decimal SumEdv;
        public decimal SumTepl;
        public decimal SumSv;
        public SzSum()
        {
            ClearSum();
        }
        public void AddSum(SzGilec sg)
        {
            SumLgota += sg.SumLgota;
            SumSubs += sg.SumSubs;
            SumEdv += sg.SumEdv;
            SumSv += sg.SumSv;
        }

        public void ClearSum()
        {
            SumLgota = 0;
            SumSubs = 0;
            SumEdv = 0;
            SumSv = 0;
        }
    }

    /// <summary>
    /// Жилец получающий льготы
    /// </summary>
    public class SzGilec : SzSum
    {
        public string Fam;
        public string Ima;
        public string Otch;
        public string DatRog;

        public SzGilec()
        {
            Clear();
        }
        public string FIO
        {
            get
            {
                return string.Format("{0} {1} {2}", Fam, Ima, Otch);
            }
        }

        public void Clear()
        {
            Fam = String.Empty;
            Ima = String.Empty;
            Otch = String.Empty;
            DatRog = String.Empty;
            ClearSum();
        }
    }

    /// <summary>
    /// Информация по льготам в счете на оплату
    /// </summary>
    public class SzInformation : SzSum
    {
        public List<SzGilec> ListGilec;

        public SzInformation()
        {
            ListGilec = new List<SzGilec>();
        }

        public void AddGilec(SzGilec sg)
        {
            ListGilec.Add(sg);
            AddSum(sg);
        }

        public void Clear()
        {
            ListGilec.Clear();
            ClearSum();
        }
    }


    /// <summary>
    /// Базовый класс счета квитанции, от которого наследуются все квитанции
    /// </summary>
    public class BaseFactura
    {
        /// <summary>
        /// 
        /// </summary>
        public int CountGil; // количесво фактически проживающих
        public int CountRegisterGil; // количесво проживающих по прописке
        public int CountDepartureGil; // количесво верменно выбывших
        public int CountArriveGil; // количесво верменно прибывших
        public int CountDomGil; // количесво проживающих в доме
        public int CountGilWithoutArrived; // количесво проживающих без учета временно прибывших

        public decimal FullSquare; //Общая площадь
        public decimal LiveSquare; //Жилая площадь
        public decimal CalcSquare; //Расчетная площадь
        public decimal HeatSquare; //Отопительная площадь
        public decimal DomSquare; //Общая площадь дома
        public string RashDpuPu; //Общая площадь дома
        public decimal MopSquare; //площадь мест общего пользования дома
        public string Stage; //этаж
        public decimal SumTicket; // К оплате в штрих-коде, для самары могут отличаться

        public bool Ownflat; // Приватизированная квартира ( Истина - да, ложь - нет)
        public bool IsolateFlat; //Изолированная квартира ( Истина - да, ложь - нет)
        public string PayerFio; // Фамилия квартиросъёмщика, собственника

        public int Month; //Месяц за который выдана счет-квитанция
        public int Year; //Год за который выдана счет-квитанция
        public string MonthPredlog;//Наименование месяца в предложном падеже
        public string FullMonthName;//Полное наименование месяца в счете
        public string Pkod; //Код плательщика
        public string Typek; //Признак жилое/нежилое
        public string PkodKapr; //Платежный код для капремонта Самары
        public string TarifKapr; //Тариф для капремонта Самары
        public string LicSchet; //Лицевой счет
        public string NumLs; // Номер Лицевой счет
        public string Geu; //ЖЭУ
        public string Ud; //УД в самаре
        public string Town;//Город
        public string Rajon; //Район
        public string Ulica;//Наименование улицы
        public string NumberDom;//Наименование дома и корпуса
        public string Indecs;//Индекс дома
        public string NumberFlat;//Наименование квартиры
        public string NumberRoom;//Наименование комнаты
        public string PrefixUk;//Короткий код префикс УК (от 1 до 4-х символов)
        public string CodeUk;//12 символьный код УК
        public string GilPeriods;//Периоды временного убытия
        public string DateOplat;//Дата последней оплаты по ЛС
        public decimal LastSumOplat;//Сумма последней оплаты по ЛС

        public int NzpArea; //Код территории, к которой принадлежит ЛС
        public int NzpGeu;//Код участка, к которой принадлежит ЛС
        public int BaseDom; //Базовый дом для секции

        public string AreaName;//Фио руководителя УК
        public string AreaDirectorFio;//Фио руководителя УК
        public string AreaDirectorPost;//Должность руководителя УК
        public string AreaAdsPhone;//Телефон аварийно диспетчерской службы УК
        public string AreaPhone;//Телефон УК
        public string AreaAdr;//Адрес УК
        public string AreaEmail;//Адрес email УК
        public string AreaWeb;//Адрес вебсайта УК
        public string AreaFax;//Факс УК
        public string AreaWorkTime;//Режим работы УК

        public string GeuPhone;//Телефон ЖЭУ
        public string GeuAdr;//Адрес ЖЭУ
        public string GeuName;//Наименование ЖЭУ
        public string GeuPref;//Префикс наименование ЖЭУ (ТО, ЖЭУ, РЦ)
        public string GeuDatPlat;//День, до которого следует оплатить счет
        public string GeuKodErc;//Код УК для Казани
        public string UpravDom;//Старший по домам


        public string ArendNumAct; //Последовательный номер акта для Арендаторов
        public string ArendNumDog; //Номер договора с Арендатором
        public string ArendDatDog; //Дата договора с Арендатором
        public string ArendInnDog; //ИНН Арендатора
        public string ArendKppDog; //КПП Арендатора
        public string ArendFullName; //Полное наименование Арендатора
        public string ArendUrAdr; //Юридический адрес Арендатора
        public string Pref; //префикс схемы БД

        public string Shtrih;//Штрихкод для счета

        public FakturaBlockTable FakturaBlocks;

        public decimal HvsNorm; //Норма по ХВС
        public decimal KanNormCalc; //Норма по канализации из параметра
        public decimal GvsNorm; //Норма по ГВС
        public decimal GvsNormGkal; //Норма по ГВС
        public decimal OtopNorm; //Норма по отоплению
        public decimal HvsGvsNorma;//Норма по ХВС для ГВС
        public string KfodnEl; //коэффициетн нормы ОДН по электроснабжению
        public string Kfodnhvs; //коэффициетн нормы ОДН по холодному водоснабжению
        public string Kfodngvs; //коэффициетн нормы ОДН по горячему водоснабжению

        public Faktura.WorkFakturaRegims BillRegim; //Режим формирования лицевого счета

        public string AreaRemark; //Примечание УК в счете
        public string GeuRemark;//Примечание ЖЭУ в счете
        public string DomRemark;//Примечание по Дому в счете

        public bool HasElDpu; //Наличие домового прибора учета по электроэнергии
        public bool HasHvsDpu; //Наличие домового прибора учета по холодной воде
        public bool HasGvsDpu; //Наличие домового прибора учета по горячей воде
        public bool HasOtopDpu; //Наличие домового прибора учета по отоплению
        public bool HasGazDpu; //Наличие домового прибора учета по газу
        public bool HasOpenVodozabor; //Открытый водозабор горячей воды 

        public decimal SumEdv; //Сумма ЕДВ для СЗ
        public decimal SumLgota; //Сумма льготы
        public decimal SumSmo; //Сумма субсидий малообеспеченным
        public decimal SumTepl; //Сумма субсидий на тепло

#warning Костыль
        public decimal RashodOdpuGkal; // Расход ОДПУ ГВС Гкал
        public decimal RevalOtopl307; //Сумма корректировки за год по отоплению для Самары

        public _Rekvizit Rekvizit; //Банковские реквизиты лицевого счета
        public CUnionServ CUnionServ; //Правила объединения услуг
        public BaseServ SummaryServ; //Итого в счете, в т.ч. к оплате
        public List<BaseServ> ListGilServ; //Жилищные услуги
        public List<BaseServ> ListKommServ; //Коммунальные услуги
        public List<BaseServ> ListServ; //Список услуг счета
        public List<BaseServ> ListSupp; //Список поставщиков
        public List<ServReval> ListReval; //Список оснований перерасчета
        public List<ServVolume> ListVolume; //Список расходов по услугам
        public List<Counters> ListCounters; //Список счетчиков по услугам по ЛС
        public List<Counters> ListDomCounters; //Список счетчиков по услугам по дому

        public List<FakturaCounters> NewlistCounters; //Список счетчиков по услугам по ЛС
        public List<FakturaCounters> NewlistDomCounters; //Список счетчиков по услугам по дому

        public SzInformation SzInformation;
        public DbFakturaCounters DbfakturaCounters;
        public DbFakturaCharge DbfakturaCharge;
        public DbFakturaReval DbfakturaReval;
        public DbFakturaOrdering DbfakturaOrdering;

        public BaseFactura()
        {

            Rekvizit = new _Rekvizit();
            SummaryServ = new BaseServ(false);
            ListServ = new List<BaseServ>();
            ListSupp = new List<BaseServ>();
            ListReval = new List<ServReval>();
            ListVolume = new List<ServVolume>();
            ListCounters = new List<Counters>();
            ListDomCounters = new List<Counters>();
            CUnionServ = new CUnionServ();
            ListGilServ = new List<BaseServ>();
            ListKommServ = new List<BaseServ>();
            SzInformation = new SzInformation();
            NewlistCounters = new List<FakturaCounters>();
            NewlistDomCounters = new List<FakturaCounters>();
            DbfakturaCharge = null;

            FakturaBlocks.HasAdrBlock = true;
            FakturaBlocks.HasRekvizitBlock = true;
            FakturaBlocks.HasKvarPrmBlock = true;
            FakturaBlocks.HasSummuryBillBlock = true;
            FakturaBlocks.HasMainChargeGridBlock = true;
            FakturaBlocks.HasRevalReasonBlock = false;
            FakturaBlocks.HasServiceVolumeBlock = false;
            FakturaBlocks.HasCountersBlock = false;
            FakturaBlocks.HasCountersDoubleBlock = false;
            FakturaBlocks.HasCountersDoubleDomBlock = false;
            FakturaBlocks.HasRTCountersBlock = false;
            FakturaBlocks.HasRTCountersDoubleBlock = false;
            FakturaBlocks.HasRTCountersDoubleDomBlock = false;
            FakturaBlocks.HasSupplierPkod = false;

            FakturaBlocks.HasOdnBlock = false;
            FakturaBlocks.HasRdnBlock = false;
            FakturaBlocks.HasPerekidkiSamaraBlock = false;
            FakturaBlocks.HasNormblock = false;
            FakturaBlocks.HasGilPeriodsBlock = false;
            FakturaBlocks.HasDatOplBlock = false;
            FakturaBlocks.HasCountersSpisBlock = false;
            FakturaBlocks.HasAreaDataBlock = false;
            FakturaBlocks.HasGeuDataBlock = false;
            FakturaBlocks.HasDomRashodBlock = false;
            Clear();
        }


        /// <summary>
        /// Добавление услуги в счета
        /// </summary>
        /// <param name="aServ">Услуга</param>
        public virtual void AddServ(BaseServ aServ)
        {
            if (aServ.Empty())
                return;
            foreach (BaseServ baseServ in ListKommServ)
            {
                if (baseServ.Serv.NzpServ == aServ.Serv.NzpServ)
                    aServ.KommServ = true;
            }
            SummaryServ.AddSum(aServ.Serv);
            if (Math.Abs(aServ.Serv.Reval) > new Decimal(1, 0, 0, false, (byte)3) || Math.Abs(aServ.Serv.RealCharge) > new Decimal(1, 0, 0, false, (byte)3))
                this.AddReval(aServ.Serv);
            BaseServ mainServBySlave = CUnionServ.GetMainServBySlave(aServ.Serv.NzpServ);
            if (mainServBySlave == null || aServ.Serv.NzpServ == 14)
            {
                bool flag = false;
                foreach (BaseServ baseServ in ListServ)
                {
                    if (baseServ.Serv.NzpServ == aServ.Serv.NzpServ)
                    {
                        baseServ.AddSum(aServ.Serv);
                        if (baseServ.Serv.NameSupp == "")
                        {
                            baseServ.Serv.NameSupp = aServ.Serv.NameSupp;
                            baseServ.Serv.SuppRekv = aServ.Serv.SuppRekv;
                        }
                        else if (aServ.Serv.Tarif > new Decimal(0))
                        {
                            baseServ.Serv.NameSupp = aServ.Serv.NameSupp;
                            baseServ.Serv.SuppRekv = aServ.Serv.SuppRekv;
                            if (!aServ.Serv.IsOdn)
                                baseServ.Serv.NameServ = aServ.Serv.NameServ;
                        }
                        flag = true;
                    }
                }
                if (flag)
                    return;
                this.ListServ.Add(aServ);
            }
            else
            {
                bool flag = false;
                foreach (BaseServ baseServ in this.ListServ)
                {
                    if (baseServ.Serv.NzpServ == mainServBySlave.Serv.NzpServ)
                    {
                        if (baseServ.Serv.NzpServ == 25 && aServ.Serv.NzpServ == 515 && (baseServ.Serv.Tarif == 2.45m || baseServ.Serv.Tarif == 0m))
                            baseServ.Serv.NameServ = "Электроэнергия день";
                        if (aServ.Serv.IsOdn)
                            aServ.Serv.CanAddTarif = false;
                        if (aServ.Serv.Tarif > new Decimal(0))
                        {
                            baseServ.Serv.NameSupp = aServ.Serv.NameSupp;
                            baseServ.Serv.SuppRekv = aServ.Serv.SuppRekv;
                            if (!aServ.Serv.IsOdn)
                                baseServ.Serv.NameServ = aServ.Serv.NameServ;
                        }
                        baseServ.AddSum(aServ.Serv);
                        flag = true;
                    }
                }
                if (!flag)
                {
                    
                    BaseServ baseServ = (BaseServ)mainServBySlave.Clone();
                    baseServ.KommServ = aServ.KommServ;
                    if (aServ.Serv.IsOdn)
                        aServ.Serv.CanAddTarif = false;
                    if (aServ.Serv.Tarif > new Decimal(0))
                    {
                        baseServ.Serv.NameSupp = aServ.Serv.NameSupp;
                        baseServ.Serv.SuppRekv = aServ.Serv.SuppRekv;
                        if (!aServ.Serv.IsOdn)
                        {
                            baseServ.Serv.NameServ = aServ.Serv.NameServ;
                            baseServ.Serv.IsDevice = aServ.Serv.IsDevice;
                        }
                    }
                    if (aServ.Serv.NzpServ == 515)
                        baseServ.Serv.NameServ = "Электроэнергия день";
                    baseServ.Serv.Measure = aServ.Serv.Measure;
                    baseServ.AddSum(aServ.Serv);
                    this.ListServ.Add(baseServ);
                }
            }
        }

        /// <summary>
        /// Добавление услуги в счета
        /// </summary>
        /// <param name="aServ">Услуга</param>
        public virtual void AddSupp(BaseServ aServ)
        {

            //Проверка на пустоту добавляемой услуги
            if (aServ.Empty()) return;
            bool findSupp = false;


            foreach (BaseServ t in ListSupp)
            {
                if (t.Serv.NzpSupp == aServ.Serv.NzpSupp) //если такой поставщик уже есть то добавляем к нему
                {
                    t.AddSum(aServ.Serv);
                    t.Serv.NameSupp = aServ.Serv.NameSupp;
                    t.Serv.SuppRekv = aServ.Serv.SuppRekv;
                    t.Serv.NameServ += ", " + aServ.Serv.NameServ;
                    findSupp = true;
                }
            }
            if (!findSupp)
            {
                var newServ = new BaseServ(false);
                newServ.AddSum(aServ.Serv);
                newServ.Serv.NameSupp = aServ.Serv.NameSupp;
                newServ.Serv.SuppRekv = aServ.Serv.SuppRekv;
                newServ.Serv.NameServ = aServ.Serv.NameServ;
                ListSupp.Add(newServ); //иначе добавляем 
            }

        }

        public virtual void AddReval(SumServ aServ)
        {
            ServReval aReval;
            bool findServ = false;


            for (int i = 0; i < ListReval.Count; i++)
            {
                if (ListReval[i].NzpServ == aServ.NzpServ)
                {
                    aReval = ListReval[i];
                    aReval.SumReval += aServ.Reval + aServ.RealCharge;
                    aReval.CReval += aServ.CReval;
                    aReval.SumGilReval += aServ.RevalGil;
                    ListReval[i] = aReval;
                    findServ = true;
                }
            }

            if (findServ == false)
            {
                aReval = new ServReval
                {
                    NzpServ = aServ.NzpServ,
                    ServiceName = aServ.NameServ,
                    SumReval = aServ.Reval + aServ.RealCharge,
                    CReval = aServ.CReval,
                    SumGilReval = aServ.RevalGil
                };

                ListReval.Add(aReval);
            }



        }

        public virtual void AddCounters(Counters aCounter)
        {
            var aCount = new Counters
            {
                NzpServ = aCounter.NzpServ,
                ServiceName = aCounter.ServiceName,
                Value = aCounter.Value,
                DatUchet = aCounter.DatUchet,
                ValuePred = aCounter.ValuePred,
                DatUchetPred = aCounter.DatUchetPred,
                NumCounters = aCounter.NumCounters,
                CntStage = aCounter.CntStage,
                Place = aCounter.Place,
                DatProv = aCounter.DatProv,
                IsGkal = aCounter.IsGkal,
                Measure = aCounter.Measure
            };
            ListCounters.Add(aCount);
        }

        public virtual void AddDomCounters(Counters aCounter)
        {
            var aCount = new Counters
            {
                NzpServ = aCounter.NzpServ,
                ServiceName = aCounter.ServiceName,
                Value = aCounter.Value,
                DatUchet = aCounter.DatUchet,
                ValuePred = aCounter.ValuePred,
                DatUchetPred = aCounter.DatUchetPred,
                NumCounters = aCounter.NumCounters,
                CntStage = aCounter.CntStage,
                DatProv = aCounter.DatProv,
                IsGkal = aCounter.IsGkal,
                Measure = aCounter.Measure
            };
            ListDomCounters.Add(aCount);
        }

        public virtual void AddReasonReval(int nzpServ, string reason, string period)
        {
            for (int i = 0; i < ListReval.Count; i++)
            {
                if (ListReval[i].NzpServ == nzpServ)
                {
                    ServReval aReval = ListReval[i];
                    if (aReval.Reason != null)
                    {
                        aReval.Reason += "," + reason;
                        aReval.ReasonPeriod += "," + period;
                    }
                    else
                    {
                        aReval.Reason = reason;
                        aReval.ReasonPeriod = period;
                    }
                    ListReval[i] = aReval;
                }
                //if ((nzpServ == 9) & (listReval[i].nzpServ == 14))
                //{
                //    aReval = listReval[i];
                //    if (aReval.reason != null)
                //    {
                //        aReval.reason += "," + reason;
                //        aReval.reasonPeriod += "," + period;
                //    }
                //    else
                //    {
                //        aReval.reason = reason;
                //        aReval.reasonPeriod = period;
                //    }
                //    listReval[i] = aReval;

                //}
            }




        }

        public virtual void AddVolume(ServVolume aVolume)
        {
            ListVolume.Add(aVolume);
        }

        public virtual void AddDomVolume(ServVolume aVolume)
        {

            int i = 0;
            bool findServ = false;


            while ((i < ListVolume.Count) & (findServ == false))
            {
                if (ListVolume[i].NzpServ == aVolume.NzpServ)
                {
                    ListVolume[i].DomArendatorsVolume = 0;
                    ListVolume[i].DomArendatorsVolume = aVolume.DomArendatorsVolume;
                    ListVolume[i].DomLiftVolume = aVolume.DomLiftVolume;
                    ListVolume[i].DomVolume = aVolume.DomVolume;
                    ListVolume[i].OdnDomVolume = aVolume.OdnDomVolume;
                    ListVolume[i].Kf307 = aVolume.Kf307;
                    ListVolume[i].AllLsVolume = aVolume.AllLsVolume;
                    findServ = true;
                }
                i++;
            }

            if (findServ == false) ListVolume.Add(aVolume);
        }


        public virtual bool IsShowServInGrid(BaseServ aServ)
        {
            return true;
        }

        public virtual void AddPerekidkaOdn(int nzpServ, decimal sumRcl)
        {

            int i = 0;
            const bool findServ = false;

            while ((i < ListServ.Count) & (findServ == false))
            {
                if (ListServ[i].Serv.NzpServ == nzpServ)
                {
                    BaseServ servPerekidka = ListServ[i];
                    servPerekidka.ServOdn.RsumTarif += sumRcl;
                    servPerekidka.Serv.RealCharge -= sumRcl;
                    servPerekidka.Serv.RsumTarif += sumRcl;
                    SummaryServ.Serv.RealCharge -= sumRcl;
                    SummaryServ.Serv.RsumTarif += sumRcl;   // добавил Андрей Кайнов 19.12.2012
                    // корректировка итого по колонке "Всего начислено" на размер платы на общедомовые нужды

                    servPerekidka.ServOdn.SumCharge += sumRcl;
                    SummaryServ.ServOdn.RsumTarif += sumRcl;
                    SummaryServ.ServOdn.SumCharge += sumRcl;
                }
                i++;
            }

        }


        /// <summary>
        /// Процедура проставляет расход по услугам из calc_gku
        /// </summary>
        /// <returns></returns>
        protected virtual bool SetServRashod()
        {
            decimal kanNorma = 0;
            decimal gvsNorm = 0;

            int numhvsgvs = -1;
            StreamWriter streamWriter = new StreamWriter("C:\\temp\\FillMainChargeGrid2.txt", true);
            foreach (ServVolume t in ListVolume)
            {
                streamWriter.WriteLine(t.NzpServ);
                for (int j = 0; j < ListServ.Count; j++)
                {
                    if ((t.NzpServ == ListServ[j].Serv.NzpServ) & (t.NzpServ != 8) &
                        (t.NzpServ != 7))
                    {
                        if (t.IsPu > 0)
                        {
                            ListServ[j].Serv.CCalc = t.PUVolume;
                            ListServ[j].ServOdn.CCalc = t.OdnFlatPuVolume;
                        }
                        else
                        {

                            ListServ[j].Serv.CCalc = t.NormaFullVolume;
                            ListServ[j].ServOdn.CCalc = t.OdnFlatNormVolume;
                        }

                        ListServ[j].Serv.Norma = t.NormaVolume;

                        if (ListServ[j].Serv.NzpServ == 14)
                        {
                            numhvsgvs = j;
                            gvsNorm = t.NormaVolume;
                        }
                        if (ListServ[j].Serv.NzpServ == 6)
                        {
                            kanNorma = t.NormaVolume;
                        }

                    }
                }
            }

            //Если есть ХВС для ГВС
            if (numhvsgvs > -1)
            {
                //Норматив 9-ке проставляем кубометровый
                foreach (ServVolume t in ListVolume)
                    if (t.NzpServ == 9) t.NormaVolume = gvsNorm;


                //Норматив канализации проставляем как сумму ХВС и ГВС
                foreach (ServVolume t in ListVolume)
                    if (t.NzpServ == 7) t.NormaVolume = gvsNorm + kanNorma;
            }


            //Для канализации проставляем кубометры в нормативе если расчет с человека
            foreach (BaseServ t in ListServ)
            {
                if ((t.Serv.NzpServ == 7) & (t.Serv.OldMeasure == 2))
                {
                    t.Serv.Norma = kanNorma;
                    t.Serv.CCalc = t.Serv.CCalc * kanNorma;
                }
            }
            streamWriter.Close();
            return true;

        }


        /// <summary>
        /// Заполнение адреса
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected virtual bool FillAdr(DataRow dr)
        {
            if (dr == null) return false;
            dr["Platelchik"] = PayerFio;
            if (Ulica.ToUpper().Contains("ПРОЕЗД") || Ulica.ToUpper().Contains("ПРОСЕК"))
                Ulica = "УЛ. " + Ulica;
            dr["ulica"] = Ulica;
            dr["numdom"] = NumberDom;
            dr["kvnum"] = NumberFlat;
            if (IsolateFlat)
            {
                dr["kv_pl"] = FullSquare.ToString("0.00");
            }
            else
            {
                dr["kv_pl"] = LiveSquare.ToString("0.00");
            }
            return true;
        }


        /// <summary>
        /// Заполнение адреса
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected virtual bool FillCalcGil(DataRow dr)
        {
            if (dr == null) return false;
            dr["countGil"] = CountGil + CountArriveGil;
            dr["countDepartureGil"] = CountDepartureGil;
            dr["countArriveGil"] = CountArriveGil;
            return true;
        }

        /// <summary>
        /// Заполнение банковских реквизитов и реквизитов счета
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected virtual bool FillRekvizit(DataRow dr)
        {
            if (dr == null) return false;
            dr["str_rekv1"] = Rekvizit.poluch2.Replace("Общество с ограниченной ответственностью", "ООО");
            dr["str_rekv2"] = Rekvizit.poluch2 + ", фактич. адрес " +
                            Rekvizit.adres2 + ", тел." + Rekvizit.phone2 +
                        "; р/с " + Rekvizit.rschet2 + " в " + Rekvizit.bank2;
            dr["str_rekv3"] = Rekvizit.poluch + " ИНН-" + Rekvizit.inn;
            dr["str_rekv4"] = "Р/с - " + Rekvizit.rschet + "   Кор/счет-" + Rekvizit.korr_schet + "  " +
                        "  БИК " + Rekvizit.bik + " " + Rekvizit.bank;
            dr["Data_dolg"] = "01." + Month.ToString("00") + "." + Year;
            dr["month_"] = Month;
            dr["year_"] = Year;
            dr["months"] = FullMonthName;
            return true;
        }


        /// <summary>
        /// Заполнение квартирных параметров
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected virtual bool FillKvarPrm(DataRow dr)
        {
            StreamWriter sw = new StreamWriter(@"C:\temp\people1.txt", false);
            sw.WriteLine("1");
            sw.Close();
            try
            {
                //if (dr == null) return false;
                if (Ownflat)
                {
                    dr["priv"] = "Приватизирована";
                }
                else
                {
                    dr["priv"] = "не приватизирована";
                }
                dr["kolgil2"] = CountRegisterGil;
                dr["kolgil"] = CountGil + CountArriveGil - CountDepartureGil;
                //dr["ls"] = Pkod.Substring(5, 5);
                //if (Pkod.Substring(10, 1) == "0")
                //    dr["ls"] = Pkod.Substring(5, 5);
                //else
                //    dr["ls"] = Pkod.Substring(5, 5) + " " + Pkod.Substring(10, 1);

                //if (NzpGeu > 100)
                    dr["ngeu"] = NzpGeu.ToString().Substring(1, 2);
               // else
                    //dr["ngeu"] = Pkod.Substring(3, 2);
                dr["indecs"] = Indecs;
                dr["ud"] = Ud;
                dr["num_ls"] = NumLs;
                dr["pkod"] = Pkod;
                dr["pl_dom"] = DomSquare.ToString("0.00");
                dr["pl_mop"] = MopSquare.ToString("0.00");//MopSquare
                decimal otopDpu = (DomSquare - MopSquare) * Convert.ToDecimal(RashDpuPu);
                dr["dom_gil"] = CountDomGil.ToString();
                decimal otopDpu2;
                try
                {
                    otopDpu2 = (DomSquare - MopSquare) * Convert.ToDecimal(RashDpuPu.Replace('.', ','));
                }
                catch
                {
                    otopDpu2 = (DomSquare - MopSquare) * Convert.ToDecimal(RashDpuPu);
                }
                dr["rash_dpu_pu_otop"] = otopDpu2.ToString("0.00");
                return true;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Заполнение итоговой строки по счету
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected virtual bool FillSummuryBill(DataRow dr)
        {
            if (dr == null) return false;
            dr["rsum_tarif"] = SummaryServ.Serv.RsumTarif.ToString("0.00");
            dr["sum_tarif"] = SummaryServ.Serv.SumTarif.ToString("0.00");
            dr["reval"] = SummaryServ.Serv.Reval.ToString("0.00");
            dr["reval_charge"] = (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge).ToString("0.00");
            dr["real_charge"] = SummaryServ.Serv.RealCharge.ToString("0.00");
            dr["sum_charge_all"] = SummaryServ.Serv.SumCharge.ToString("0.00");
            dr["sum_charge"] = (SummaryServ.Serv.SumCharge - SummaryServ.ServOdn.SumCharge).ToString("0.00");
            dr["sum_charge_odn"] = SummaryServ.ServOdn.SumCharge.ToString("0.00");
            dr["sum_insaldo"] = SummaryServ.Serv.SumInsaldo.ToString("0.00");
            dr["sum_money"] = SummaryServ.Serv.SumMoney.ToString("0.00");
            dr["sum_ticket"] = SumTicket.ToString("0.00"); 
            dr["sum_rub"] = Decimal.Truncate(SumTicket).ToString("0");
            dr["sum_kop"] = ((SumTicket % 1) * 100).ToString("0");


            return true;
        }

        /// <summary>
        /// Заполнение данных таблицы начислений
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected virtual bool FillMainChargeGrid(DataRow dr)
        {
            StreamWriter streamWriter = new StreamWriter("C:\\temp\\FillMainChargeGrid2.txt", true);
            streamWriter.WriteLine("1");
            streamWriter.Close();
            if (dr == null) return false;
            SetServRashod();
            int numberString = 1;
            foreach (BaseServ t in ListServ)
            {
                if (IsShowServInGrid(t))
                {
                    dr["name_serv" + numberString] = t.Serv.NameServ + ",";
                    //Преобразование, если расчет стоит с человека
                    if ((t.Serv.NzpServ == 9) & (t.Serv.NzpMeasure != 3) &
                        (GvsNorm > 0.001m))
                    {
                        t.Serv.Measure = "куб.м";
                        decimal cCalcOld = t.Serv.CCalc -
                                           t.ServOdn.CCalc;
                        decimal oldRSumTarif = t.Serv.RsumTarif -
                                               t.ServOdn.RsumTarif;

                        if (cCalcOld > 0.0001m)
                        {
                            t.Serv.Tarif = (oldRSumTarif / cCalcOld) / GvsNorm;
                            t.Serv.CCalc = oldRSumTarif / t.Serv.Tarif;
                            t.ServOdn.CCalc = t.ServOdn.RsumTarif
                                              / t.Serv.Tarif;
                        }
                        else
                        {
                            if ((t.Serv.SumCharge == 0) & (
                                t.ServOdn.SumCharge == 0)) t.Serv.Tarif = 0;
                        }
                    }
                    if ((t.Serv.NzpServ == 6) & (t.Serv.NzpMeasure != 3) &
                        (HvsNorm > 0.001m))
                    {
                        t.Serv.Measure = "куб.м";
                        decimal cCalcOld = t.Serv.CCalc -
                                           t.ServOdn.CCalc;
                        decimal oldRSumTarif = t.Serv.RsumTarif -
                                               t.ServOdn.RsumTarif;

                        if (cCalcOld > 0.0001m)
                        {
                            t.Serv.Tarif = (oldRSumTarif / cCalcOld) / HvsNorm;
                            t.Serv.CCalc = oldRSumTarif / t.Serv.Tarif;
                            t.ServOdn.CCalc = t.ServOdn.RsumTarif
                                              / t.Serv.Tarif;
                        }
                        else
                        {
                            if ((t.Serv.SumCharge == 0) & (
                                t.ServOdn.SumCharge == 0)) t.Serv.Tarif = 0;

                        }
                    }

                    if ((t.Serv.NzpServ == 7) & (t.Serv.NzpMeasure != 3) &
                        (HvsNorm + GvsNorm > 0.001m))
                    {
                        t.Serv.Measure = "куб.м";
                        decimal cCalcOld = t.Serv.CCalc -
                                           t.ServOdn.CCalc;
                        decimal oldRSumTarif = t.Serv.RsumTarif -
                                               t.ServOdn.RsumTarif;

                        if (cCalcOld > 0.0001m)
                        {
                            t.Serv.Tarif = (oldRSumTarif / cCalcOld) / (HvsNorm + GvsNorm);
                            t.Serv.CCalc = oldRSumTarif / t.Serv.Tarif;
                            t.ServOdn.CCalc = t.ServOdn.RsumTarif
                                              / t.Serv.Tarif;
                        }
                        else
                        {
                            if ((t.Serv.SumCharge == 0) & (
                                t.ServOdn.SumCharge == 0)) t.Serv.Tarif = 0;

                        }
                    }

                    dr["measure" + numberString] = t.Serv.Measure;


                    if ((Math.Abs(t.Serv.CCalc) > 0.001m) &
                        (t.Serv.IsOdn == false))
                    {

                        if ((t.Serv.RsumTarif ==
                             t.ServOdn.RsumTarif) & (t.Serv.RsumTarif > 0.001m))
                        {
                        }
                        else
                        {
                            dr["c_calc" + numberString] = t.Serv.CCalc.ToString("0.0000");
                        }

                    }


                    //Добавляем 4 знака после запятой
                    /*   if (listServ[countServ].servOdn.cCalc > 0.0001m)
                    {
                        for (int k = 0; k<listVolume.Count; k++)
                            if ((listVolume[k].nzpServ == listServ[countServ].serv.nzpServ)&(
                                System.Math.Abs(listVolume[k].odnFlatPuVolume + listVolume[k].odnFlatNormVolume)>0.0001m))
                            {
                                listServ[countServ].servOdn.cCalc = listVolume[k].odnFlatPuVolume == 0 ? listVolume[k].odnFlatNormVolume : listVolume[k].odnFlatPuVolume;
                            }

                    }*/

                    if (Math.Abs(t.ServOdn.CCalc) > 0.001m)
                    {
                        dr["c_calc_odn" + numberString] = t.ServOdn.CCalc.ToString("0.0000");
                    }

                    dr["tarif" + numberString] = t.Serv.Tarif.ToString("0.000");
                    dr["rsum_tarif" + numberString] = (t.Serv.RsumTarif -
                                                       t.ServOdn.RsumTarif).ToString("0.00");
                    dr["rsum_tarif_odn" + numberString] = t.ServOdn.RsumTarif.ToString("0.00");
                    dr["rsum_tarif_all" + numberString] = t.Serv.RsumTarif.ToString("0.00");
                    dr["reval" + numberString] = (t.Serv.Reval +
                                                  t.Serv.RealCharge).ToString("0.00");
                    if (Math.Abs(t.Serv.Reval + t.Serv.RealCharge) > 0.001m)
                    {
                        dr["revalnull" + numberString] = (t.Serv.Reval +
                                                          t.Serv.RealCharge).ToString("0.00");
                    }
                    else
                    {
                        dr["revalnull" + numberString] = "";
                    }
                    dr["sum_lgota" + numberString] = "";
                    dr["sum_charge_all" + numberString] = t.Serv.SumCharge.ToString("0.00");
                    dr["sum_charge" + numberString] = (t.Serv.SumCharge -
                                                       t.ServOdn.SumCharge).ToString("0.00");
                    dr["sum_charge_odn" + numberString] = t.ServOdn.SumCharge.ToString("0.00");

                    dr["sum_nedop" + numberString] = t.Serv.SumNedop.ToString("0.00");
                    dr["sum_sn" + numberString] = t.Serv.SumSn.ToString("0.00");
                    dr["sum_outsaldo" + numberString] = t.Serv.SumOutsaldo.ToString("0.00");
                    dr["real_charge" + numberString] = t.ServOdn.RealCharge.ToString("0.00");
                    numberString++;
                }
            }


            return true;
        }

        /// <summary>
        ///  Заполняет одну строку в таблице начислений
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="stringIndex">Номер строки в таблице начислений</param>
        /// <param name="bs">Услуга</param>
        /// <param name="numSt"></param>
        /// <returns></returns>
        protected virtual bool FillOneRowInChargeGrid(DataRow dr, int stringIndex, BaseServ bs, string numSt)
        {
            return true;
        }

        /// <summary>
        /// Заполнение счетчиков
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public virtual bool FillCounters(DataRow dr)
        {
            return true;
        }

        /// <summary>
        /// Заполнение реквизитов территории
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public virtual bool FillAreaData(DataRow dr)
        {
            if (dr == null) return false;
            dr["areaAdsPhone"] = AreaAdsPhone;
            dr["areaAdr"] = AreaAdr;
            dr["areaDirectorFio"] = AreaDirectorFio;
            dr["areaDirectorPost"] = AreaDirectorPost;
            dr["areaEmail"] = AreaEmail;
            dr["areaWeb"] = AreaWeb;
            dr["areaPhone"] = AreaPhone;
            return true;
        }

        /// <summary>
        /// Заполнение реквизитов ЖЭУ
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public virtual bool FillGeuData(DataRow dr)
        {
            if (dr == null) return false;
            dr["geuAdr"] = AreaAdr;
            dr["geuName"] = GeuName;
            dr["geuPref"] = GeuPref;
            dr["geuDatPlat"] = GeuDatPlat;
            dr["geuPhone"] = GeuPhone;
            return true;
        }

        /// <summary>
        /// Заполнение блока старшего по дому
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public virtual bool FillUpravDom(DataRow dr)
        {
            if (dr == null) return false;
            dr["upravDom"] = UpravDom;
            return true;
        }


        /// <summary>
        /// Заполнение номера и даты Акта
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public virtual bool FillNumArend(DataRow dr)
        {
            if (dr == null) return false;
            dr["num_arend"] = ArendNumAct;
            dr["dat_arend"] = DateTime.DaysInMonth(Year, Month).ToString("00") + Month.ToString("00") + Year;
            return true;
        }


        /// <summary>
        /// Заполнение расходов по дому
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public virtual bool FillDomRashod(DataRow dr)
        {
            if (dr == null) return false;
            foreach (ServVolume t in ListVolume)
            {
                if ((Math.Abs(t.DomVolume) > 0.00001m) ||
                    (Math.Abs(t.OdnDomVolume) > 0.00001m) ||
                    (Math.Abs(t.DomArendatorsVolume) > 0.00001m))

                    switch (t.NzpServ)
                    {
                        case 6:
                        {
                            dr["hv_dpu"] = t.DomVolume.ToString("0.00##");
                            dr["hv_dpu_odn"] = t.OdnDomVolume.ToString("0.00##");
                            dr["hv_arend"] = t.DomArendatorsVolume.ToString("0.00##");
                            if (t.Kf307 != 0)
                                dr["k_hv"] = t.Kf307.ToString("0.0000##");
                        }
                            break;
                        case 9:
                        {
                            dr["gv_dpu"] = t.DomVolume.ToString("0.00##");
                            dr["gv_dpu_odn"] = t.OdnDomVolume.ToString("0.00##");
                            dr["gv_arend"] = t.DomArendatorsVolume.ToString("0.00##");
                            if (t.Kf307 != 0)
                                dr["k_gv"] = t.Kf307.ToString("0.0000##");
                        } break;
                        case 8:
                        {
                            dr["otop_dpu"] = t.DomVolume.ToString("0.00##");
                            dr["otop_dpu_odn"] = t.OdnDomVolume.ToString("0.00##");
                            dr["otop_arend"] = t.DomArendatorsVolume.ToString("0.00##");
                        } break;
                        case 25:
                        {
                            dr["el_dpu"] = t.DomVolume.ToString("0.00##");
                            dr["el_dpu_odn"] = t.OdnDomVolume.ToString("0.00##");
                            dr["el_arend"] = t.DomArendatorsVolume.ToString("0.00##");
                            if (t.Kf307 != 0)
                                dr["k_el"] = t.Kf307.ToString("0.0000##");
                        } break;
                        case 210:
                        {
                            dr["ni_dpu"] = t.DomVolume.ToString("0.00##");
                            dr["ni_dpu_odn"] = t.OdnDomVolume.ToString("0.00##");
                            dr["ni_arend"] = t.DomArendatorsVolume.ToString("0.00##");
                        } break;
                    }
            }
            return true;
        }

        /// <summary>
        /// Заполнение домовых счетчиков
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public virtual bool FillDomCounters(DataRow dr)
        {
            return true;
        }

        /// <summary>
        /// Заполнение рассрочки
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public virtual bool FillRassrochka(DataRow dr)
        {
            return true;
        }

        /// <summary>
        /// Заполнение причин перерасчета
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected virtual bool FillRevalReason(DataRow dr)
        {
            if (dr == null) return false;

            int kanNumber = -1;
            string hvsReason = "";
            string gvsReason = "";
            for (int i = 0; i < 9; i++)
            {
                if (ListReval.Count > i)
                {
                    dr["serv_pere" + (i + 1)] = ListReval[i].ServiceName;
                    if (ListReval[i].NzpServ == 7) kanNumber = i + 1;
                    if (ListReval[i].Reason == null)
                    {
                        if (Math.Abs(ListReval[i].CReval) < 0.001m)
                        {
                            if (Math.Abs(ListReval[i].SumGilReval) < 0.001m)
                            {

                                dr["osn_pere" + (i + 1)] = "Изменение тарифа/недопоставка ";
                            }
                            else
                                dr["osn_pere" + (i + 1)] = "Временное выбытие жильца" + GilPeriods;

                        }
                        else
                        {
                            dr["osn_pere" + (i + 1)] = "Изменение расхода по услуге";
                        }
                    }
                    else
                        dr["osn_pere" + (i + 1)] = ListReval[i].Reason;
                    dr["sum_pere" + (i + 1)] = ListReval[i].SumReval;
                    dr["period_pere" + (i + 1)] = "";
                    if (ListReval[i].NzpServ == 6) hvsReason = dr["osn_pere" + (i + 1)].ToString();
                    if (ListReval[i].NzpServ == 9) gvsReason = dr["osn_pere" + (i + 1)].ToString();
                }
                else
                {
                    dr["serv_pere" + (i + 1)] = "";
                    dr["osn_pere" + (i + 1)] = "";
                    dr["sum_pere" + (i + 1)] = "";
                    dr["period_pere" + (i + 1)] = "";

                }
            }
            if (kanNumber > -1)
            {
                if (hvsReason != "")
                    dr["osn_pere" + kanNumber] = hvsReason;
                else
                    dr["osn_pere" + kanNumber] = gvsReason;
            }

            return true;
        }

        protected virtual void FillNullServVolume(DataRow dr, int index)
        {
            dr["rash_name" + index] = "";
            dr["rash_norm" + index] = "";
            dr["rash_norm_odn" + index] = "";
            dr["rash_pu" + index] = "";
            dr["rash_pu_odn" + index] = "";
            dr["rash_dpu_pu" + index] = "";
            dr["rash_dpu_odn" + index] = "";

        }

        protected virtual void FillGoodServVolume(DataRow dr, decimal aValue, string colName)
        {

            if (aValue < 0.001m)
            {
                dr[colName] = "";
            }
            else
            {
                dr[colName] = aValue.ToString("0.00##");
            }
        }

        /// <summary>
        /// Заполнение расходов по услугам
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected virtual bool FillServiceVolume(DataRow dr)
        {
            if (dr == null)
                return false;
            int num = 1;
            int count = this.ListServ.Count;
            foreach (ServVolume servVolume in this.ListVolume)
            {
                Decimal aValue = servVolume.NormaVolume;
                for (int index = 0; index < count; ++index)
                {
                    if (this.ListServ[index].Serv.NzpServ == servVolume.NzpServ && this.ListServ[index].Serv.Tarif <= new Decimal(1, 0, 0, false, (byte)4))
                    {
                        aValue = new Decimal(0);
                        break;
                    }
                }
                if (Math.Abs(aValue) + Math.Abs(servVolume.OdnFlatPuVolume) + Math.Abs(servVolume.OdnFlatNormVolume) + Math.Abs(servVolume.PUVolume) > new Decimal(1, 0, 0, false, (byte)4) & num < 11)
                {
                    dr["rash_name" + (object)num] = (object)servVolume.ServiceName;
                    this.FillGoodServVolume(dr, aValue, "rash_norm" + (object)num);
                    if (servVolume.NzpServ == 25 & this.KfodnEl != "")
                        dr["rash_norm_odn" + (object)num] = (object)this.KfodnEl;
                    this.FillGoodServVolume(dr, servVolume.OdnFlatNormVolume, "rash_pu_odn" + num.ToString());
                    this.FillGoodServVolume(dr, servVolume.DomVolume, "rash_dpu_pu" + (object)num);
                    this.FillGoodServVolume(dr, servVolume.OdnDomVolume, "rash_dpu_odn" + (object)num);
                    bool flag = false;
                    for (int index = 0; index < this.ListCounters.Count; ++index)
                    {
                        if (this.ListCounters[index].NzpServ == servVolume.NzpServ & num < 11)
                        {
                            this.FillGoodServVolume(dr, this.ListCounters[index].Value, "rash_pu" + (object)num);
                            ++num;
                            flag = true;
                        }
                    }
                    if (!flag)
                        ++num;
                }
            }
            for (int index = num; index < 11; ++index)
                this.FillNullServVolume(dr, index);
            return true;
        }

        /// <summary>
        /// Заполнение штрих-кода
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected virtual bool FillBarcode(DataRow dr)
        {
            if (dr == null) return false;
            dr["vars"] = GetBarCode();
            return true;
        }

        protected virtual bool FillQrCode(DataRow dr)
        {
            if (dr == null) return false;
            dr["datamatrix"] = GetQRCode();
            StreamWriter sw = new StreamWriter(@"C:\Temp\QrCode3.txt");
            sw.WriteLine(GetQRCode());
            sw.Close();
            return true;
        }

        /// <summary>
        /// Заполнение Примечания в счете
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected virtual bool FillRemark(DataRow dr)
        {
            if (dr == null) return false;
            if (DomRemark == null) DomRemark = "";
            if (GeuRemark == null) GeuRemark = "";
            if (AreaRemark == null) AreaRemark = "";
            if (DomRemark.Length > 0) dr["remark"] = DomRemark;
            else if (GeuRemark.Length > 0) dr["remark"] = GeuRemark;
            else if (AreaRemark.Length > 0) dr["remark"] = AreaRemark;

            dr["dom_remark"] = DomRemark;
            dr["geu_remark"] = GeuRemark;
            dr["area_remark"] = AreaRemark;
            return true;
        }

        /// <summary>
        /// дата последней оплаты по счету
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected virtual bool FillDatOpl(DataRow dr)
        {
            if (dr == null) return false;

            dr["dat_opl"] = DateOplat;
            dr["sum_last_opl"] = LastSumOplat.ToString("0.00");
            return true;
        }

        /// <summary>
        /// Загрузка информации от СЗ
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected virtual bool FillSzInf(DataRow dr)
        {
            if (dr == null) return false;

            return true;
        }

        public virtual string GetBarCode()
        {
            return "0";
        }

        public virtual string GetQRCode()
        {
            return "0";
        }

        public virtual bool DoPrint()
        {
            if (ListServ.Count == 0) return false;
            if (Points.Region == Regions.Region.Samarskaya_obl && PkodKapr == "") return false;         
            return true;
        }

        /// <summary>
        /// Создание перечня всех полей счета
        /// </summary>
        /// <returns></returns>
        public virtual DataTable MakeTable()
        {
            var table = new DataTable {TableName = "Q_master"};
            table.Columns.Add("str_rekv1", typeof(string));
            table.Columns.Add("str_rekv2", typeof(string));
            table.Columns.Add("str_rekv3", typeof(string));
            table.Columns.Add("str_rekv4", typeof(string));
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("typek", typeof(string));
            table.Columns.Add("Platelchik", typeof(string));
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("numdom", typeof(string));
            table.Columns.Add("kvnum", typeof(string));
            table.Columns.Add("kv_pl", typeof(string));
            table.Columns.Add("priv", typeof(string));
            table.Columns.Add("kolgil2", typeof(string));
            table.Columns.Add("kolgil", typeof(string));
            table.Columns.Add("ls", typeof(string));
            table.Columns.Add("num_ls", typeof(string));
            table.Columns.Add("indecs", typeof(string));
            table.Columns.Add("ngeu", typeof(string));
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("dom_remark", typeof(string));
            table.Columns.Add("geu_remark", typeof(string));
            table.Columns.Add("area_remark", typeof(string));
            table.Columns.Add("vars", typeof(string));
            table.Columns.Add("rsum_tarif", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("sum_ticket", typeof(string));
            table.Columns.Add("sum_charge_all", typeof(string));
            table.Columns.Add("sum_charge_odn", typeof(string));
            table.Columns.Add("sum_insaldo", typeof(string));
            table.Columns.Add("sum_money", typeof(string));
            table.Columns.Add("sum_rub", typeof(string));
            table.Columns.Add("sum_kop", typeof(string));
            table.Columns.Add("ud", typeof(string));
            table.Columns.Add("Data_dolg", typeof(string));
            table.Columns.Add("months", typeof(string));
            table.Columns.Add("month_", typeof(string));
            table.Columns.Add("year_", typeof(string));
            table.Columns.Add("sum_tarif", typeof(string));
            table.Columns.Add("reval", typeof(string));
            table.Columns.Add("real_charge", typeof(string));
            table.Columns.Add("reval_charge", typeof(string));

            table.Columns.Add("areaAdsPhone", typeof(string));
            table.Columns.Add("areaAdr", typeof(string));
            table.Columns.Add("areaDirectorFio", typeof(string));
            table.Columns.Add("areaDirectorPost", typeof(string));
            table.Columns.Add("areaEmail", typeof(string));
            table.Columns.Add("areaWeb", typeof(string));
            table.Columns.Add("areaPhone", typeof(string));

            table.Columns.Add("geuPhone", typeof(string));
            table.Columns.Add("geuAdr", typeof(string));
            table.Columns.Add("geuName", typeof(string));
            table.Columns.Add("geuPref", typeof(string));
            table.Columns.Add("geuDatPlat", typeof(string));
            table.Columns.Add("upravDom", typeof(string));

            table.Columns.Add("num_arend", typeof(string));
            table.Columns.Add("dat_arend", typeof(string));

            table.Columns.Add("el_dpu", typeof(string));
            table.Columns.Add("el_dpu_odn", typeof(string));
            table.Columns.Add("el_arend", typeof(string));
            table.Columns.Add("ni_dpu", typeof(string));
            table.Columns.Add("ni_dpu_odn", typeof(string));
            table.Columns.Add("ni_arend", typeof(string));
            table.Columns.Add("hv_dpu", typeof(string));
            table.Columns.Add("hv_dpu_odn", typeof(string));
            table.Columns.Add("hv_arend", typeof(string));
            table.Columns.Add("gv_dpu", typeof(string));
            table.Columns.Add("gv_dpu_odn", typeof(string));
            table.Columns.Add("gv_arend", typeof(string));
            table.Columns.Add("otop_dpu", typeof(string));
            table.Columns.Add("otop_dpu_odn", typeof(string));
            table.Columns.Add("otop_arend", typeof(string));
            table.Columns.Add("gv_dpu_gkal", typeof(string));
            table.Columns.Add("k_el", typeof(string));
            table.Columns.Add("k_hv", typeof(string));
            table.Columns.Add("k_gv", typeof(string));
            table.Columns.Add("rash_dpu_pu_otop", typeof(string));

            for (int i = 1; i < 20; i++)
            {
                table.Columns.Add("name_serv" + i, typeof(string));
                table.Columns.Add("measure" + i, typeof(string));
                table.Columns.Add("c_calc" + i, typeof(string));
                table.Columns.Add("c_calc_odn" + i, typeof(string));
                table.Columns.Add("tarif" + i, typeof(string));
                table.Columns.Add("rsum_tarif" + i, typeof(string));
                table.Columns.Add("rsum_tarif_odn" + i, typeof(string));
                table.Columns.Add("rsum_tarif_all" + i, typeof(string));
                table.Columns.Add("reval" + i, typeof(string));
                table.Columns.Add("revalnull" + i, typeof(string));
                table.Columns.Add("sum_lgota" + i, typeof(string));
                table.Columns.Add("sum_charge_all" + i, typeof(string));
                table.Columns.Add("sum_charge" + i, typeof(string));
                table.Columns.Add("sum_charge_odn" + i, typeof(string));
                table.Columns.Add("sum_nedop" + i, typeof(string));
                table.Columns.Add("sum_sn" + i, typeof(string));
                table.Columns.Add("sum_outsaldo" + i, typeof(string));
                table.Columns.Add("real_charge" + i, typeof(string));

            }

            for (int i = 1; i < 10; i++)
            {
                table.Columns.Add("serv_pere" + i, typeof(string));
                table.Columns.Add("osn_pere" + i, typeof(string));
                table.Columns.Add("sum_pere" + i, typeof(string));
            }
            for (int i = 1; i < 15; i++)
            {
                table.Columns.Add("rash_name" + i, typeof(string));
                table.Columns.Add("rash_norm" + i, typeof(string));
                table.Columns.Add("rash_norm_odn" + i, typeof(string));
                table.Columns.Add("rash_pu" + i, typeof(string));
                table.Columns.Add("rash_pu_odn" + i, typeof(string));
                table.Columns.Add("rash_dpu_pu" + i, typeof(string));
                table.Columns.Add("rash_dpu_odn" + i, typeof(string));
            }

            return table;

        }

        /// <summary>
        /// Характерные для конкретного вида счета процедуры обработки данных счета
        /// </summary>
        /// <param name="finder"></param>
        public virtual void FinalPass(Faktura finder)
        {
        }

        /// <summary>
        /// Характерные для конкретного вида счета процедуры обработки данных счета
        /// </summary>
        public virtual void OrderPrint(int nzpKvar)
        {
        }

        /// <summary>
        /// Заполение 1 строки резульирующей таблицы данными ЛС
        /// </summary>
        /// <param name="dt">результирующая таблица</param>
        /// <returns></returns>
        public virtual bool FillRow(DataTable dt)
        {
            StreamWriter sw = new StreamWriter(@"C:\temp\people12345678.txt", false);
            try
            {
                DataRow dr = dt.NewRow();
                sw.WriteLine("1");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasAdrBlock");
                if (FakturaBlocks.HasAdrBlock) FillAdr(dr);
                sw.WriteLine("2");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasRekvizitBlock");
                if (FakturaBlocks.HasRekvizitBlock) FillRekvizit(dr);
                sw.WriteLine("3");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasKvarPrmBlock");
                /*if (FakturaBlocks.HasKvarPrmBlock)*/ FillKvarPrm(dr);
                sw.WriteLine("4");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasMainChargeGridBlock");
                if (FakturaBlocks.HasMainChargeGridBlock) FillMainChargeGrid(dr);
                sw.WriteLine("5");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasRevalReasonBlock");
                if (FakturaBlocks.HasRevalReasonBlock) FillRevalReason(dr);
                sw.WriteLine("6");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasServiceVolumeBlock");
                if (FakturaBlocks.HasServiceVolumeBlock) FillServiceVolume(dr);
                sw.WriteLine("7");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasRassrochka");
                if (FakturaBlocks.HasRassrochka) FillRassrochka(dr);
                sw.WriteLine("8");
                try
                {
                    if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasCountersBlock");
                    if (FakturaBlocks.HasCountersBlock) FillCounters(dr);
                    sw.WriteLine("9");
                }
                catch (Exception)
                {

                }
                try
                {
                    if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasNewCountersBlock");
                    if (FakturaBlocks.HasNewCountersBlock) FillCounters(dr);
                    sw.WriteLine("10");
                }
                catch (Exception)
                {

                }
                try
                {
                    if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasNewDoubleCountersBlock");
                    if (FakturaBlocks.HasNewDoubleCountersBlock) FillCounters(dr);
                    sw.WriteLine("11");
                }
                catch (Exception)
                {

                }
                try
                {
                    if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasCountersDoubleBlock");
                    if (FakturaBlocks.HasCountersDoubleBlock) FillCounters(dr);
                    sw.WriteLine("11_1");
                }
                catch (Exception)
                {

                }
                
                
                sw.WriteLine("12");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasCountersDoubleDomBlock");
                if (FakturaBlocks.HasCountersDoubleDomBlock) FillDomCounters(dr);
                sw.WriteLine("13");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasRemarkblock");
                if (FakturaBlocks.HasRemarkblock) FillRemark(dr);
                sw.WriteLine("14");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasDatOplBlock");
                if (FakturaBlocks.HasDatOplBlock) FillDatOpl(dr);
                sw.WriteLine("15");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasSzBlock");
                if (FakturaBlocks.HasSzBlock) FillSzInf(dr);
                sw.WriteLine("16");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasAreaDataBlock");
                if (FakturaBlocks.HasAreaDataBlock) FillAreaData(dr);
                sw.WriteLine("17");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasGeuDataBlock");
                if (FakturaBlocks.HasGeuDataBlock) FillGeuData(dr);
                sw.WriteLine("18");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasUpravDomBlock");
                if (FakturaBlocks.HasUpravDomBlock) FillUpravDom(dr);
                sw.WriteLine("19");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasDomRashodBlock");
                if (FakturaBlocks.HasDomRashodBlock) FillDomRashod(dr);
                sw.WriteLine("20");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasArendBlock");
                if (FakturaBlocks.HasArendBlock) FillNumArend(dr);
                sw.WriteLine("21");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasCalcGil");
                if (FakturaBlocks.HasCalcGil) FillCalcGil(dr);
                sw.WriteLine("22");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("FillSummuryBill");
                FillSummuryBill(dr);
                sw.WriteLine("23");
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("FillBarcode");
                FillBarcode(dr);
                if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("FillQRcode");
                FillQrCode(dr);
                sw.WriteLine("24");
                if (FakturaBlocks.HasRTCountersDoubleBlock)
                    FillCounters(dr);
                sw.WriteLine("25");
                //if (FakturaBlocks.HasRTCountersDoubleDomBlock)
                if (1==1)
                    FillDomCounters(dr);
                sw.WriteLine("26");
                sw.Close();
                dt.Rows.Add(dr);
                return true;
            }
            catch
            {
                sw.Close();
                return true;
            }
        }

        public virtual void Clear()
        {
            CountGil = 0;
            CountRegisterGil = 0;
            CountDepartureGil = 0;
            CountArriveGil = 0;
            CountDomGil = 0;

            FullSquare = 0;
            LiveSquare = 0;
            CalcSquare = 0;
            HeatSquare = 0;
            DomSquare = 0; //Общая площадь дома
            MopSquare = 0; //площадь мест общего пользования дома
            RashDpuPu = "";

            HasOpenVodozabor = false;

            AreaDirectorFio = "";
            AreaDirectorPost = "";
            AreaAdsPhone = "";
            AreaPhone = "";
            AreaAdr = "";
            AreaEmail = "";
            AreaWeb = "";

            GeuPhone = "";
            GeuAdr = "";
            GeuName = "";
            GeuDatPlat = "";
            GeuPref = "";
            GeuKodErc = "";

            Ownflat = false;
            BaseDom = 0;
            IsolateFlat = true;
            PayerFio = "";
            Kfodnhvs = "";
            Kfodngvs = "";
            OtopNorm = 0;
            HvsGvsNorma = 0;
            GvsNormGkal = 0;
            KanNormCalc = 0;
            SumEdv = 0;
            SumLgota = 0;
            SumSmo = 0;
            SumTepl = 0;
            Indecs = "";

            Month = DateTime.Now.Month;
            Year = DateTime.Now.Year;
            FullMonthName = "";
            Pkod = "";
            PkodKapr = "";
            LicSchet = "";
            Geu = "";
            Ud = "";
            Ulica = "";
            Town = "";
            NumberDom = "";
            NumberFlat = "";
            PrefixUk = "";
            CodeUk = "";
            GilPeriods = "";
            DateOplat = "";
            LastSumOplat = 0;
            UpravDom = "";
            Pref = "";

            NzpArea = 0;
            NzpGeu = 0;

            HasElDpu = false;
            HasHvsDpu = false;
            HasGvsDpu = false;
            HasOtopDpu = false;
            HasGazDpu = false;


            SummaryServ.Clear();
            ListServ.Clear();
            ListSupp.Clear();
            ListReval.Clear();
            ListVolume.Clear();
            ListCounters.Clear();
            ListDomCounters.Clear();
            SzInformation.Clear();
            NewlistCounters.Clear();
            NewlistDomCounters.Clear();

            Stage = "";
            Shtrih = "";
            ArendNumAct = "";


            Rekvizit.nzp_geu = 0;
            Rekvizit.nzp_area = 0;
            Rekvizit.poluch = "";
            Rekvizit.bank = "";
            Rekvizit.rschet = "";
            Rekvizit.korr_schet = "";
            Rekvizit.bik = "";
            Rekvizit.inn = "";
            Rekvizit.phone = "";
            Rekvizit.adres = "";
            Rekvizit.pm_note = "";
            Rekvizit.poluch2 = "";
            Rekvizit.bank2 = "";
            Rekvizit.rschet2 = "";
            Rekvizit.korr_schet2 = "";
            Rekvizit.bik2 = "";
            Rekvizit.inn2 = "";
            Rekvizit.phone2 = "";
            Rekvizit.adres2 = "";
            Rekvizit.filltext = 0;
            BillRegim = Faktura.WorkFakturaRegims.One;
            if (DbfakturaCounters != null) DbfakturaCounters.Clear();
            if (DbfakturaCharge != null) DbfakturaCharge.Clear();
            if (DbfakturaReval != null) DbfakturaReval.Clear();
            if (DbfakturaOrdering != null) DbfakturaOrdering.Clear();

        }


    }




}



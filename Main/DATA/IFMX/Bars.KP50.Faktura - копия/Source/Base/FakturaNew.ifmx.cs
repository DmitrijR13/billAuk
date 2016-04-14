
using System;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Faktura.Source.Base
{
    /// <summary>
    /// Базовый класс счета квитанции, от которого наследуются все квитанции
    /// </summary>
    public class BaseFactura2 : BaseBill
    {

        /// <summary>Название счета-квитанции</summary>
        public override string Name
        {
            get { return "Базовый"; }
        }

        /// <summary>Описание счета-квитанции</summary>
        public override string Description
        {
            get { return ""; }
        }

        public override string FileName { get { return "demo.frx"; } }

        public override string Ulica //Наименование улицы
        {
            set
            {
                if (value.Length > 2)
                {
                    _ulica = value.Substring(value.Length - 3, 3) == "/ -" ? value.Substring(0, value.Length - 3).Trim() : value;
                }
                else
                {
                    _ulica = value;
                }
            }
            get { return _ulica; }
        }

        public override string NumberDom //Наименование дома и корпуса
        {
            set { _numberDom = value.TrimEnd('-');}
            get { return _numberDom; }
        }
        public string NumberFlat //Наименование квартиры и комнаты
        {
            set
            {
               _numberFlat = value.TrimEnd('-');
               _numberFlat = _numberFlat.Trim();
                if (_numberFlat.Equals("0")) _numberFlat = "";
            }
            get { return _numberFlat; }
        }
        public override string NumberRoom //Наименование дома и корпуса
        {
            set { _numberRoom = value.TrimEnd('-'); }
            get { return _numberRoom; }
        }

        public DbFakturaRekvizit Rekvizits;
        public DbFakturaSzInformation SzInf;
        public DbFakturaAreaPrm Area;
        public DbFakturaGeuPrm Geu;
        public DbFakturaDomPrm Dom;
        public DbFakturaKvarPrm Kvar;
        public DbFakturaArendPrm Arendator;
        public DbFakturaCounters Counters;
        public DbFakturaCharge Charge;
        public DbFakturaReval Reval;
        public DbFakturaServVolumeDom ServVolumeDom;
        public DbFakturaServVolumeLs ServVolumeLs;
        public DbFakturaPayments Payments;
        public DbFakturaInstalment354 Instalment;
        public DbFakturaSpravInformation SpravInf;

        protected DataRow Dr;

        private string _ulica;
        private string _numberFlat;
        private string _numberRoom;
        private string _numberDom;
        public virtual void Init(IDbConnection connection, int year, int month)
        {
            Rekvizits = new DbFakturaRekvizit(connection);
            SzInf = new DbFakturaSzInformation(connection, month, year);
            Area = new DbFakturaAreaPrm(connection, month, year);
            Geu = new DbFakturaGeuPrm(connection);
            Dom = new DbFakturaDomPrm(connection, month, year);
            Kvar = new DbFakturaKvarPrm(connection, month, year);
            Arendator = new DbFakturaArendPrm(connection, month, year);
            Counters = new DbFakturaCounters(connection, month, year);
            Charge = new DbFakturaCharge(connection, month, year);
            Reval = new DbFakturaReval(connection, month, year);
            ServVolumeDom = new DbFakturaServVolumeDom(connection, month, year);
            ServVolumeLs = new DbFakturaServVolumeLs(connection, month, year);
            Payments = new DbFakturaPayments(connection, month, year);
            Instalment = new DbFakturaInstalment354(connection, month, year);
            SpravInf = new DbFakturaSpravInformation(connection, month, year);
            Clear();
        }




        public virtual bool IsShowServInGrid(BaseServ2 aServ)
        {
            return true;
        }

  
       

        /// <summary>
        /// Заполнение адреса
        /// </summary>
        /// <returns></returns>
        protected virtual bool FillAdr()
        {
            if (Dr == null) return false;
            return true;
        }


        /// <summary>
        /// Заполнение адреса
        /// </summary>
        /// <returns></returns>
        protected virtual bool FillCalcGil()
        {
            if (Dr == null) return false;
             return true;
        }

        /// <summary>
        /// Заполнение банковских реквизитов и реквизитов счета
        /// </summary>
        /// <returns></returns>
        protected virtual bool FillRekvizit()
        {
            if (Dr == null) return false;
            return true;
        }


        /// <summary>
        /// Заполнение квартирных параметров
        /// </summary>
        /// <returns></returns>
        protected virtual bool FillKvarPrm()
        {
            if (Dr == null) return false;
            return true;
        }

        /// <summary>
        /// Заполнение итоговой строки по счету
        /// </summary>
        /// <returns></returns>
        protected virtual bool FillSummuryBill()
        {
            if (Dr == null) return false;
            return true;
        }

        /// <summary>
        /// Заполнение данных таблицы начислений
        /// </summary>
        /// <returns></returns>
        protected virtual bool FillMainChargeGrid()
        {
            if (Dr == null) return false;
            return true;
        }

        /// <summary>
        ///  Заполняет одну строку в таблице начислений
        /// </summary>
        /// <param name="stringIndex">Номер строки в таблице начислений</param>
        /// <param name="bs">Услуга</param>
        /// <param name="numSt"></param>
        /// <returns></returns>
        protected virtual bool FillOneRowInChargeGrid(int stringIndex, BaseServ2 bs, string numSt)
        {
            return true;
        }

        /// <summary>
        /// Заполнение счетчиков
        /// </summary>
        /// <returns></returns>
        public virtual bool FillCounters()
        {
            return true;
        }

        /// <summary>
        /// Заполнение реквизитов территории
        /// </summary>
        /// <returns></returns>
        public virtual bool FillAreaData()
        {
            return Dr != null;
        }

        /// <summary>
        /// Заполнение реквизитов ЖЭУ
        /// </summary>
        /// <returns></returns>
        public virtual bool FillGeuData()
        {
            return Dr != null;
        }

        /// <summary>
        /// Заполнение блока по дому
        /// </summary>
        /// <returns></returns>
        public virtual bool FillDomPrm()
        {
            return Dr != null;
        }


        /// <summary>
        /// Заполнение данных арендаторов
        /// </summary>
        /// <returns></returns>
        public virtual bool FillArend()
        {
            return Dr != null;
        }

        /// <summary>
        /// Заполнение домовых счетчиков
        /// </summary>
        /// <returns></returns>
        public virtual bool FillDomCounters()
        {
            return true;
        }

        /// <summary>
        /// Заполнение рассрочки
        /// </summary>
        /// <returns></returns>
        public virtual bool FillRassrochka()
        {
            return true;
        }

        /// <summary>
        /// Заполнение причин перерасчета
        /// </summary>
        /// <returns></returns>
        protected virtual bool FillRevalReason()
        {
            return Dr != null;
        }


        /// <summary>
        /// Заполнение расходов по услугам
        /// </summary>
        /// <returns></returns>
        protected virtual bool FillServiceVolume()
        {
            return Dr != null;
        }

        /// <summary>
        /// Заполнение штрих-кода
        /// </summary>
        /// <returns></returns>
        protected virtual bool FillBarcode()
        {
            return Dr != null;
        }

        /// <summary>
        /// Заполнение Примечания в счете
        /// </summary>
        /// <returns></returns>
        protected virtual bool FillRemark()
        {
            return true;
        }

        /// <summary>
        /// дата последней оплаты по счету
        /// </summary>
        /// <returns></returns>
        protected virtual bool FillDatOpl()
        {
            return Dr != null;
        }

        /// <summary>
        /// Загрузка информации от СЗ
        /// </summary>
        /// <returns></returns>
        protected virtual bool FillSzInf()
        {
            return Dr != null;
        }

        /// <summary>
        /// Заполнение дополнительной информации
        /// </summary>
        /// <returns></returns>
        protected virtual bool FillSpravInf()
        {
            return Dr != null;
        }

        public virtual string GetBarCode()
        {
            return "0";
        }

        public virtual bool DoPrint()
        {
            if ((Charge.ListServ.Count == 0)&
                (BillRegim != STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.One )) return false;
            return true;
        }

        /// <summary>
        /// Создание перечня всех полей счета
        /// </summary>
        /// <returns></returns>
        public override DataTable MakeTable()
        {
            var table = new DataTable { TableName = "Q_master" };
            table.Columns.Add("test_field", typeof(string));
            return table;
        }

        /// <summary>
        /// Создание таблиц епд
        /// </summary>
        /// <returns></returns>
        public virtual DataSet MakeFewTables()
        {
            var tables = new DataSet();
            tables.Tables.Add(MakeTable());
            return tables;
        }

        /// <summary>
        /// Характерные для конкретного вида счета процедуры обработки данных счета
        /// </summary>
        /// <param name="finder"></param>
        public virtual void FinalPass(STCLINE.KP50.Interfaces.Faktura finder)
        {
        }

        /// <summary>
        /// Заполение 1 строки резульирующей таблицы данными ЛС
        /// </summary>
        /// <param name="dt">результирующая таблица</param>
        /// <returns></returns>
        public override bool FillRow(DataTable dt)
        {
            Dr = dt.NewRow();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasAdrBlock");
            FillAdr(); 
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasRekvizitBlock");
            FillRekvizit();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasKvarPrmBlock");
            FillKvarPrm();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasDomPrmBlock");
            FillDomPrm();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasMainChargeGridBlock");
            FillMainChargeGrid();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasRevalReasonBlock");
            FillRevalReason();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasServiceVolumeBlock");
            FillServiceVolume();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasRassrochka");
            FillRassrochka();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasCountersBlock");
            FillCounters();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasCountersDoubleDomBlock");
            FillDomCounters();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasRemarkblock");
            FillRemark();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasDatOplBlock");
            FillDatOpl();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasSzBlock");
            FillSzInf();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasSpravInfBlock");
            FillSpravInf();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasAreaDataBlock");
            FillAreaData();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasGeuDataBlock");
            FillGeuData();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasArendBlock");
            FillArend();
            var finder = new STCLINE.KP50.Interfaces.Faktura();
            FinalPass(finder);
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("hasSummaryBlock");
            FillSummuryBill();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("FillSummuryBill");
            FillBarcode();
            if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("FillBarcode");

            if (DoPrint()) dt.Rows.Add(Dr); 

            return true;
        }        
        
        /// <summary>
        /// Заполение таблиц данными 
        /// </summary>
        /// <param name="dt">датасет с таблицами епд</param>
        /// <returns></returns>
        public virtual bool FillTables(DataSet ds)
        {
            return FillRow(ds.Tables[0]); 
        }

        public override void Clear()
        {

            Month = DateTime.Now.Month;
            Year = DateTime.Now.Year;
            FullMonthName = String.Empty;
            Pkod = String.Empty;
            Ud = String.Empty;
            _ulica = String.Empty;
            _numberDom = String.Empty;
            _numberFlat = String.Empty;
            
            NzpArea = 0; //Код территории, к которой принадлежит ЛС
            NzpGeu = 0;//Код участка, к которому принадлежит ЛС
            NzpDom = 0;//Код дома, к которому принадлежит ЛС
            NzpKvar = 0;//Код квартиры
            NumLs = 0;//Код ЛС
            Pkod = String.Empty; //Код плательщика

            BillRegim = STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.One;
            Kvar.Clear();
            Arendator.Clear();
            Counters.Clear();
            Charge.Clear();
            Reval.Clear();
            ServVolumeLs.Clear();
            SzInf.Clear();
            Payments.Clear();
        }


       



    }
   
}



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.REPORT
{
    //класс для хранения всех параметров, передаваемых функции
    //для потоков
    public class ParamContainer
    {
        //список домовых или квартирных параметров
        public List<Prm> listprm { set; get; }
        public List<ExcelSverkaPeriod> listsverkaperiod { set; get; }
        public Ls finder { set; get; }
        //пользователь
        public int nzp_user { set; get; }
        //номер квартипы
        public int nzp_kvar { set; get; }
        //месяц
        public string mm { set; get; }
        //год
        public string yy { set; get; }
        //месяц с
        public string from_mm { set; get; }
        //год с
        public string from_yy { set; get; }
        //месяц по
        public string to_mm { set; get; }
        //год по
        public string to_yy { set; get; }
        //комментарий к отчету
        public string comment { set; get; }
        //номер услуги
        public int nzp_serv { set; get; }
        //номер поставщика
        public int nzp_supp { set; get; }
        //Список параметров-начислений
        public List<int> parList { set; get; }
        //первый параметр для отчета супг
        public string _nzp { set; get; }
        //второй параметр для отчета супг
        public string _nzp_add { set; get; }
        //энумератор
        public enSrvOper en { set; get; }
        //объект для Оплат
        public Payments payments { set; get; }
        //год
        public int yearr { set; get; }
        //проверка на выборку услуг
        public bool serv { set; get; }
        //уникальный номер 
        public int nzp { set; get; }

        public SupgFinder supgfinder { get; set; }

        public ChargeUnloadPrm chargeUnloadPrm { get; set; }

        public Kart Kartfinder { get; set; }

        public KLADRFinder KLADRfinder { get; set; }

        public Finder Finder { get; set; }

        public FinderChangeArea ChangeAreaFinder { get; set; }

        public ChargeFind ChargeFind { get; set; }

        public FilesImported filesImportedFinder { get; set; }

        public ReportPrm reportPrm { get; set; }

        public Deal Deal { get; set; }

        public ReportType reportType { get; set; }

        public ExFinder ExFinder { get; set; }

        public MoneyDistrib MoneyDistrib { get; set; }
        
        public string unloadVersionFormat { get; set; }

        public ParamContainer()
        {
        }
    }
}

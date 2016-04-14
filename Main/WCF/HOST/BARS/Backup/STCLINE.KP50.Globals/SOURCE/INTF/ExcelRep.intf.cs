using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using STCLINE.KP50.Global;
using System.Data;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_ExcelRep
    {
        [OperationContract]
        Returns CreateExcelReport_host(List<Prm> listprm, int nzp_user, string comment);

        [OperationContract]
        Returns GetDomCalcs(int Nzp_user, string mm, string yy);

        [OperationContract]
        Returns GetAnalisKart(List<Prm> listprm, int Nzp_user);

        [OperationContract]
        Returns GetCalcTarif(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetDomNach(List<Prm> listprm, int Nzp_user);

        [OperationContract]
        Returns GetSaldoRep10_14_3(Prm prm_, int Nzp_user);

        [OperationContract]
        Returns GetSaldoRep10_14_1(Prm prm_, int Nzp_user);

        [OperationContract]
        Returns GetSverkaDay(ExcelSverkaPeriod prm_, int Nzp_user);

        [OperationContract]
        Returns GetSverkaMonth(ExcelSverkaPeriod prm_, int Nzp_user);

        [OperationContract]
        Returns GetCharges(Prm prm_, int Nzp_user);

        [OperationContract]
        Returns GetDomNachPere(List<Prm> listprm, int Nzp_user);

        [OperationContract]
        Returns GetSpravSuppNach(List<Prm> listprm, int Nzp_user);

        [OperationContract]
        Returns GetSpravSuppNachHar(Prm prm_, int Nzp_user);

        [OperationContract]
        Returns GetSpravSuppNachHar2(Prm prm_, int Nzp_user);

        [OperationContract]
        Returns GetSpravHasDolg(Prm finder, int Nzp_user);

        [OperationContract]
        Returns GetVerifCalcs(Ls finder, string yy_from, string mm_from, string yy_to, string mm_to);

        [OperationContract]
        Returns GetStateGilFond(string yy_from, string mm_from, string yy_to, string mm_to);

        [OperationContract]
        Returns GetDebtCalcs(Ls finder, string yy_from, string mm_from, string yy_to, string mm_to);

        [OperationContract]
        Returns GetNoticeCalcs(Ls finder, string yy, string mm);

        [OperationContract]
        Returns GetDeliveredServicesPayment(Ls finder, int nzp_supp ,string yy, string mm);

        [OperationContract]
        Returns GetLicSchetExcel(Ls finder, int year_, int month_);

        [OperationContract]
        Returns GetEnergoActSverki(Prm prm_, int nzp_supp, int Nzp_user);

        [OperationContract]
        Returns GetSpravPULs(Prm prm_, int Nzp_user);

        [OperationContract]
        Returns GetVedOplLs(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetVedPere(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetDolgSved(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetDolgSpis(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetPaspRas(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetPaspRasCommon(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetSostGilFond(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetSpravSoderg(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetSpravSoderg2(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetSpravGroupSodergGil(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetVedNormPotr(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetFakturaFiles(Prm prm, int Nzp_user);


        //[OperationContract]
        //List<Calcs> GetDomCalcs_collection(int Nzp_user, string mm, string yy);

        [OperationContract]
        Returns GetSpravPoOtklUslug(Ls finder, int nzp_serv ,int month, int year);

        [OperationContract]
        Returns GetSpravPoOtklUslugDomVinovnik(Prm prm);

        [OperationContract]
        Returns GetSpravPoOtklUslugDom(Prm prm);

        [OperationContract]
        Returns GetSpravPoOtklUslugGeuVinovnik(Prm prm);

        [OperationContract]
        byte[] GetFile(string path);

        [OperationContract]
        Returns GetInfPoRaschetNasel(Ls finder, int nzp_supp, int month, int year);

        [OperationContract]
        Returns GetReportPrmNach(Ls finder, List<int> par, int month, int year, string comment, List<int> services, bool isShowExpanded);

        [OperationContract]
        Returns GetControlDistribPayments(Payments pay);

        [OperationContract]
        Returns GetRegisterCounters(SupgFinder finder);
        
        [OperationContract]
        Returns GetSaldoServices(SupgFinder finder, int supp);

        [OperationContract]
        Returns GetNachSupp(int supp, SupgFinder finder, int yearr, bool serv);

        [OperationContract]
        Returns GetProtCalcOdn(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetProtCalcOdn2(Prm prm, int Nzp_user);

        [OperationContract]
        Returns GetSpravSuppSamara(Prm prm);

        [OperationContract]
        Returns GetSaldo_v_bank(SupgFinder finder, string year, string month, out Returns ret);

        [OperationContract]
        Returns UploadKLADRAddrSpace(KLADRFinder finder, out Returns ret);

        [OperationContract]
        Returns GenerateExchange(SupgFinder finder, out Returns ret);

        [OperationContract]
        Returns GenerateUESVigr(SupgFinder finder, out Returns ret);

        [OperationContract]
        Returns GenerateMURCVigr(SupgFinder finder, out Returns ret);

        [OperationContract]
        Returns GetUploadPU(SupgFinder finder, string year, string month, out Returns ret);

        [OperationContract]
        Returns GetSaldo_5_10(ChargeFind finder, out Returns ret);

        [OperationContract]
        Returns GetUploadReestr(Finder finder, string year, string month, out Returns ret);

        [OperationContract]
        Returns GetExchangeSZ(Finder finder, string year, string month, int nzp_ex_sz);

        [OperationContract]
        Returns GetUploadCharge(SupgFinder finder, string year, string month, out Returns ret);

        [OperationContract]
        Returns GetUploadKassa(SupgFinder finder, string year, string month, out Returns ret);


        [OperationContract]
        Returns GetSaldoVedEnergo(Prm prm);

        [OperationContract]
        Returns GetVremZareg(Kart finder);

        [OperationContract]
        Returns GetVoenkomat(Kart finder);

        [OperationContract]
        Returns GetDolgSpisEnergo(Prm prm);

        [OperationContract]
        Returns GetProtocolSverData(Finder finder);

        [OperationContract]
        Returns GetProtocolSverDataLsDom(Prm p);

        [OperationContract]
        Returns GetServSuppNach(ReportPrm prm);

        [OperationContract]
        Returns GetServSuppMoney(ReportPrm prm);

        [OperationContract]
        Returns GetCountersSprav(ReportPrm prm);

        #region Супг

        [OperationContract]
        Returns GetOrderList(int nzp_user);

        [OperationContract]
        Returns GetCountOrders(Ls finder, string _nzp, string _nzp_add, string s_date, string po_date);

        [OperationContract]
        Returns GetIncomingJobOrders(int nzp_user);

        [OperationContract]
        Returns GetPlannedWorksList(int nzp_user, enSrvOper en);

        [OperationContract]
        Returns GetSupgReports(SupgFinder finder, enSrvOper en);

        [OperationContract]
        Returns GetRepNedopList(int nzp_user, int nzp_jrn);        
        #endregion

        #region универсальный сервер отчетов

        [OperationContract]
        List<Dict> GetReportDicts(List<int> idDicts, bool loadDictsData, out Returns ret);

        #endregion

        [OperationContract]
        Returns ChangeArea(FinderChangeArea finder);


       
    }

    [DataContract]
    public class ExcelUtility : Finder   
    {
        /// <summary>
        /// Статусы задания
        /// </summary>
        public enum Statuses
        { 
            /// <summary>
            /// Состояние не определено
            /// </summary>
            None = -10,

            /// <summary>
            /// В очереди
            /// </summary>
            InQueue = 0,

            /// <summary>
            /// В процессе выполнения
            /// </summary>
            InProcess = 1,

            /// <summary>
            /// Успешно выполнено
            /// </summary>
            Success = 2,

            /// <summary>
            /// Не выполнено
            /// </summary>
            Failed = -1,

            /// <summary>
            /// Функцинал не поддерживается
            /// </summary>
            NotSupported = -2
        }

        public static Statuses GetStatusById(int id)
        {
            if (id == (int)Statuses.InQueue) return Statuses.InQueue;
            else if (id == (int)Statuses.InProcess) return Statuses.InProcess;
            else if (id == (int)Statuses.Success) return Statuses.Success;
            else if (id == (int)Statuses.Failed) return Statuses.Failed;
            else if (id == (int)Statuses.NotSupported) return Statuses.NotSupported;
            else return Statuses.None;
        }

        public static string GetStatusName(Statuses status)
        {
            switch (status)
            {
                case Statuses.InQueue: return "В очереди";
                case Statuses.InProcess: return "Формируется";
                case Statuses.Success: return "Сформирован";
                case Statuses.Failed: return "Ошибка";
                case Statuses.NotSupported: return "Режим не поддерживается";
                default: return "";
            }
        }

        public static string GetStatusNameById(int id)
        {
            Statuses status = GetStatusById(id);
            return GetStatusName(status);
        }

        public void SetStatus(Statuses status)
        {
            stats = (int)status;
        }

        [DataMember]
        public string prms { get; set; }

        [DataMember]
        public int stats { get; set; }

        [DataMember]
        public string stats_name { get; set; }

        [DataMember]
        public string dat_in { get; set; }

        [DataMember]
        public string dat_start { get; set; }

        [DataMember]
        public string dat_out { get; set; }

        [DataMember]
        public int tip { get; set; }

        [DataMember]
        public string exc_path { get; set; }

        [DataMember]
        public string exec_comment { get; set; }

        public string rep_name { get; set; }
        
        [DataMember]
        public int nzp_exc { get; set; }

        [DataMember]
        public decimal progress { get; set; }

        [DataMember]
        public int is_shared { get; set; } //общедоступность: 0 - доступен только создавшему пользователю; 1- всем 

        public Statuses status
        {
            get { return GetStatusById(stats); }
            set { SetStatus(value); }
        }

        [DataMember]
        public string status_name { 
            get 
            {
                Statuses stat = status;
                if (stat == Statuses.InProcess || stat == Statuses.Failed)
                    return GetStatusName(stat) + " (" + progress.ToString("P") + ")";
                else return GetStatusName(stat);
            }
            set { }
        }

        public ExcelUtility()
            : base()
        {
            prms = "";
            stats = 0;
            dat_in = "";
            dat_start = "";
            dat_out = "";
            tip = 0;
            exc_path = "";
            exec_comment = "";
            rep_name = "";
            stats_name = "";
            nzp_exc = 0;
            progress = 0;
            is_shared = 0;
        }
    }

    [DataContract]
    public class ExcelSverka 
    {
        [DataMember]
        public int count_kvit { get; set; }

        [DataMember]
        public decimal g_sum_ls { get; set; }

        [DataMember]
        public decimal sum_ur { get; set; }

        [DataMember]
        public decimal penya { get; set; }

        [DataMember]
        public decimal komiss { get; set; }

        [DataMember]
        public decimal tot { get; set; }

        [DataMember]
        public int nzp_area { get; set; }

        [DataMember]
        public string area { get; set; }

        public ExcelSverka()
            : base()
        {
            count_kvit = 0;
            penya = 0;
            komiss = 0;
            tot = 0;
            nzp_area = 0;
            sum_ur = 0;
            g_sum_ls = 0;
            area = "";
        }
    }

    [DataContract]
    public class ExcelSverkaPeriod: Finder
    {
        [DataMember]
        public int month_ { get; set; }

        [DataMember]
        public int year_ { get; set; }

        [DataMember]
        public string dat_s { get; set; }

        [DataMember]
        public string dat_po { get; set; }

        [DataMember]
        public int search_date { get; set; }

        [DataMember]
        public string rschet { get; set; }

        [DataMember]
        public string nzp_payer { get; set; }

        [DataMember]
        public int add_geu_column { get; set; }

        public ExcelSverkaPeriod()
            : base()
        {
            dat_s = dat_po = "";
            month_ = year_ = 0;
            search_date = 0;
            rschet = nzp_payer = "";
            add_geu_column = 0;
        }
    }

    [DataContract]
    public class Payments : Finder
    {
        [DataMember]
        public string dat_s { set; get; }
        [DataMember]
        public string dat_po { set; get; }
        [DataMember]
        public List<_Point> points { set; get; }
        [DataMember]
        public string uname { set; get; }
        [DataMember]
        public string name_user { set; get; }
        [DataMember]
        public DataSet data { set; get; }
        [DataMember]
        public int nzp_area { set; get; }
    }


    [DataContract]
    public class ReportPrm : Finder
    {
        [DataMember]
        public string reportDatBegin { set; get; }
        [DataMember]
        public string reportDatEnd { set; get; }
        [DataMember]
        public int month { set; get; }
        [DataMember]
        public int year { set; get; }
        [DataMember]
        public Dictionary<string, string> reportSuppList { set; get; }
        [DataMember]
        public Dictionary<string, string> reportServList { set; get; }
        [DataMember]
        public Dictionary<string, string> reportAreaList { set; get; }
        [DataMember]
        public Dictionary<string, string> reportGeuList { set; get; }
        [DataMember]
        public string reportName { set; get; }
        [DataMember]
        public Dictionary<string, string> reportDopParams { set; get; }
        [DataMember]
        public int nzp_dom { set; get; }
        [DataMember]
        public int nzp_kvar { set; get; }


    }


    //----------------------------------------------------------------------
    [DataContract]
    public class FinderChangeArea : Finder
    //----------------------------------------------------------------------
    {
        [DataMember]
        public bool export_saldo { get; set; }
        [DataMember]
        public bool null_square { get; set; }
        [DataMember]
        public bool null_count_gil { get; set; }
        [DataMember]
        public Area new_area { get; set; }
        [DataMember]
        public Geu new_geu { get; set; }
        [DataMember]
        public int nzp_dom { get; set; }


    }
}

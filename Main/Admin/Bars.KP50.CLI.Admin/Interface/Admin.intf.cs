using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using STCLINE.KP50.Global;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Collections;
using System.IO;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Admin
    {
        [OperationContract]
        ReturnsObjectType<BaseUser> GetUser(User finder);

        [OperationContract]
        ReturnsObjectType<List<User>> GetUsers(User finder);

        [OperationContract]
        Returns SetToChange(ServFormulFinder finder);

        [OperationContract]
        ReturnsObjectType<List<ServFormulFinder>> GetServFormul(Finder finder);

        [OperationContract]
        ReturnsObjectType<DownloadedData> GetFileGilec(Finder finder);

        [OperationContract]
        ReturnsObjectType<DownloadedData> GetFileIpu(Finder finder);

        [OperationContract]
        ReturnsObjectType<DownloadedData> GetFileIpuP(Finder finder);

        [OperationContract]
        ReturnsObjectType<DownloadedData> GetFileOdpu(Finder finder);

        [OperationContract]
        ReturnsObjectType<DownloadedData> GetFileOdpuP(Finder finder);

        [OperationContract]
        ReturnsObjectType<DownloadedData> GetFileNedopost(Finder finder);

        [OperationContract]
        ReturnsObjectType<DownloadedData> GetFileOplats(Finder finder);

        [OperationContract]
        ReturnsObjectType<DownloadedData> GetFileParamDom(Finder finder);

        [OperationContract]
        ReturnsObjectType<DownloadedData> GetFileParamLs(Finder finder);

        [OperationContract]
        ReturnsObjectType<DownloadedData> GetFileTypeNedop(Finder finder);

        [OperationContract]
        ReturnsObjectType<DownloadedData> GetFileTypeParams(Finder finder);
        
        /// <summary>
        /// Снимает все блокировки пользователя
        /// </summary>
        /// <param name="WebUserId">nzp_user Web-части</param>
        /// <returns></returns>
        [OperationContract]
        Returns RemoveUserLock(Finder WebUserId);


        
        [OperationContract]
        List<AreaCodes> GetAreaCodes(AreaCodes finder, out Returns ret);

        [OperationContract]
        Returns GetMaxCodeFromAreaCodes();

        [OperationContract]
        Returns SaveAreaCodes(AreaCodes finder);

        [OperationContract]
        Returns DeleteAreaCodes(AreaCodes finder);

        [OperationContract]
        Returns CreateSequence();

        [OperationContract]
        Returns UploadEFS(FilesImported finder);

        [OperationContract]
        List<EFSReestr> GetEFSReestr(EFSReestr finder, out Returns ret);

        [OperationContract]
        List<EFSPay> GetEFSPay(EFSPay finder, out Returns ret);

        [OperationContract]
        List<EFSCnt> GetEFSCnt(EFSCnt finder, out Returns ret);

        [OperationContract]
        Returns DeleteFromEFSReestr(EFSReestr finder);



        [OperationContract]
        List<SysEvents> GetSysEvents(SysEvents finder, out Returns ret);

        [OperationContract]
        List<SysEvents> GetSysEventsUsersList(SysEvents finder, out Returns ret);

        [OperationContract]
        List<SysEvents> GetSysEventsEventsList(SysEvents finder, out Returns ret);

        [OperationContract]
        List<SysEvents> GetSysEventsActionsList(SysEvents finder, out Returns ret);

        [OperationContract]
        List<SysEvents> GetSysEventsEntityList(SysEvents finder, out Returns ret);

        [OperationContract]
        List<CountersArx> GetCountersChangeHistory(CountersArx finder, out Returns ret);

        [OperationContract]
        List<CountersArx> GetCountersFields(CountersArx finder, out Returns ret);
        [OperationContract]
        List<CountersArx> GetCountersArxUsersList(CountersArx finder, out Returns ret);

        [OperationContract]
        bool InsertSysEvent(SysEvents finder);

        [OperationContract]
        LogsTree GetHostLogsList(LogsTree finder, out Returns ret);

        [OperationContract]
        LogsTree GetHostLogsFile(LogsTree finder, out Returns ret);

        [OperationContract]
        List<KeyValue> LoadRoleSprav(Role finder, int role_kod, out Returns ret);

        [OperationContract]
        List<TransferHome> GetHouseList(TransferHome finder, out Returns ret);
        
        [OperationContract]
        Returns PrepareProvsForFirstCalcPeni(Finder finder);
        [OperationContract]
        DateTime GetDateStartPeni(Finder finder, out Returns ret);
        [OperationContract]
        int GetCountDayToDateObligation(Finder finder, out Returns ret);
        [OperationContract]
        Returns DeleteCurrentRole(Finder finder);

        [OperationContract]
        Returns RePrepareProvsOnListLs(Ls finder, TypePrepareProvs type);
    }

    public interface IAdminRemoteObject : I_Admin, IDisposable
    {
    }

    [DataContract]
    public class KeyValue
    {
        [DataMember]
        public long key { get; set; }
        [DataMember]
        public string value { get; set; }

        public KeyValue()
            : base()
        {
            key = 0;
            value = "";
        }
    }

    [DataContract]
    public class RolesTreeNode
    {
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public int nzp_role { get; set; }
        [DataMember]
        public bool check_node { get; set; }
        [DataMember]
        public string role_name { get; set; }
        [DataMember]
        public List<RolesTreeNode> roles { get; set; }
    }

    [DataContract]
    public class RolesTree : Finder
    {
        [DataMember]
        public int regim { get; set; }
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public string user_name { get; set; }
        [DataMember]
        public int is_active { get; set; }
        [DataMember]
        public SortedDictionary<int, RolesTreeNode> SubSystemList { get; set; }
        [DataMember]
        public List<RolesTreeNode> UserRolesList { get; set; }

    }

    [DataContract]
    public class LogsTree : Finder
    {
        [DataMember]
        public int nzp_exc { get; set; }
        [DataMember]
        public string log_name { get; set; }
        [DataMember]
        public int year { get; set; }
        [DataMember]
        public int month { get; set; }
        [DataMember]
        public string month_word { get; set; }
        [DataMember]
        public int day { get; set; }
        [DataMember]
        public bool has_web_logs { get; set; }
        [DataMember]
        public string web_log_name { get; set; }
        [DataMember]
        public SortedDictionary<int, LogsTree> childs { get; set; }

        public LogsTree()
        {
            childs = new SortedDictionary<int, LogsTree>();
            has_web_logs = false;
        }
    }


    [DataContract]
    public class UserActions : Finder
    {
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public int nzp_user_act { get; set; }
        [DataMember]
        public string login { get; set; }
        [DataMember]
        public string user { get; set; }
        [DataMember]
        public int nzp_page { get; set; }
        [DataMember]
        public string page_name { get; set; }
        [DataMember]
        public int nzp_act { get; set; }
        [DataMember]
        public string act_name { get; set; }
        [DataMember]
        public Nullable<DateTime> changed_on { get; set; }
        [DataMember]
        public Nullable<DateTime> from_date { get; set; }
        [DataMember]
        public Nullable<DateTime> to_date { get; set; }
        [DataMember]
        public ArrayList pages_list { get; set; }
    }


    [DataContract]
    public class Setup : Finder
    {
        [DataMember]
        public int nzp_setup { get; set; }
        [DataMember]
        public int nzp_param { get; set; }
        [DataMember]
        public string param_name { get; set; }
        [DataMember]
        public string value { get; set; }
        [DataMember]
        public int nzpuser { get; set; }
        [DataMember]
        public string dat_when { get; set; }
        [DataMember]
        public string param_type { get; set; }

        public Setup()
            : base()
        {
            nzp_setup = 0;
            nzp_param = 0;
            param_name = "";
            value = "";
            nzpuser = 0;
            dat_when = "";
            param_type = "";
        }
    }

    [DataContract]
    public class SMTPSetup
    {
        [DataMember]
        public string host { get; set; }
        [DataMember]
        public int port { get; set; }
        [DataMember]
        public string userName { get; set; }
        [DataMember]
        public string userPwd { get; set; }
        [DataMember]
        public string fromName { get; set; }
        [DataMember]
        public string fromEmail { get; set; }



        public SMTPSetup()
        {
            host = "";
            port = 0;
            userName = "";
            userPwd = "";
            fromName = "";
            fromEmail = "";

        }
    }

    [DataContract]
    public class Email
    {
        [DataMember]
        public SMTPSetup smtp { get; set; }

        [DataMember]
        public string to { get; set; }

        [DataMember]
        public string toName { get; set; }

        [DataMember]
        public string bcc { get; set; }

        [DataMember]
        public string subject { get; set; }

        [DataMember]
        public string body { get; set; }

        [DataMember]
        public List<string> attachments { get; set; }

        public Email()
        {
            smtp = null;
            to = "";
            toName = "";
            bcc = "";
            subject = "";
            body = "";
            attachments = null;
        }
    }

    [DataContract]
    public class FileArea
    {
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
        [DataMember]
        public decimal area_number { get; set; }
        [DataMember]
        public string area { get; set; }
        [DataMember]
        public string jur_address { get; set; }
        [DataMember]
        public string fact_address { get; set; }
        [DataMember]
        public string inn { get; set; }
        [DataMember]
        public string kpp { get; set; }
        [DataMember]
        public string rs { get; set; }
        [DataMember]
        public string bank { get; set; }
        [DataMember]
        public string bik { get; set; }
        [DataMember]
        public string ks { get; set; }
        [DataMember]
        public int nzp_area { get; set; }

        public FileArea()
            : base()
        {
            id = 0;
            nzp_file = 0;
            area_number = 0;
            area = "";
            jur_address = "";
            fact_address = "";
            inn = "";
            kpp = "";
            rs = "";
            bank = "";
            bik = "";
            ks = "";
            nzp_area = 0;
        }
    }





    [DataContract]
    public class ServFormulFinder : Finder
    {
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public string serv_name { get; set; }
        [DataMember]
        public string measure_name { get; set; }
        [DataMember]
        public string supplier_name { get; set; }
        [DataMember]
        public string formul_name { get; set; }
        [DataMember]
        public bool toChange { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public int nzp_measure { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public int nzp_frm { get; set; }
    }





    [DataContract]
    public class DownloadedData
    {
        [DataMember]
        public int kol { get; set; }
        [DataMember]
        public int kol_prib { get; set; }
        [DataMember]
        public int kol_ub { get; set; }
        [DataMember]
        public decimal sum { get; set; }
    }







    [DataContract]
    public class CountersArx : Finder
    {
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public int nzp_arx { get; set; }
        [DataMember]
        public string counter_pref { get; set; }
        [DataMember]
        public int nzp_counter { get; set; }
        [DataMember]
        public string pole { get; set; }
        [DataMember]
        public string field { get; set; }
        [DataMember]
        public string val_old { get; set; }
        [DataMember]
        public string val_new { get; set; }
        [DataMember]
        public DateTime? dat_when { get; set; }
        [DataMember]
        public string dat_calc { get; set; }
        [DataMember]
        public string user { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string nzp_user_loc { get; set; }
        [DataMember]
        public DateTime? from_date { get; set; }
        [DataMember]
        public DateTime? to_date { get; set; }
        [DataMember]
        public string date_ { get; set; }
        [DataMember]
        public ArrayList users_list { get; set; }
        [DataMember]
        public ArrayList fields_list { get; set; }
        [DataMember]
        public int nzp_prm { get; set; }
    }





    [DataContract]
    public class AreaCodes : Finder
    {
        [DataMember]
        public int code { get; set; }
        [DataMember]
        public int code_s { get; set; }
        [DataMember]
        public int code_po { get; set; }
        [DataMember]
        public int nzp_area { get; set; }
        [DataMember]
        public string area { get; set; }
        [DataMember]
        public int changed_by { get; set; }
        [DataMember]
        public string changed_on { get; set; }
        [DataMember]
        public int is_active { get; set; }
        [DataMember]
        public int active_num { get; set; }
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public string active { get; set; }
        [DataMember]
        public string payer { get; set; }
        [DataMember]
        public int nzp_payer { get; set; }
        [DataMember]
        public int nzp_pkod_type { get; set; }

        public AreaCodes()
            : base()
        {
            code = 0;
            nzp_payer = 0;
            code_s = 0;
            code_po = 0;
            nzp_area = 0;
            payer = "";
            area = "";
            changed_by = 0;
            changed_on = "";
            is_active = 0;
            active_num = 0;
            num = 0;
            active = "";
        }
    }



    /// <summary>
    /// Класс для задания в очереди
    /// </summary>
    public class Job
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        [DataMember]
        public int id { get; set; }

        /// <summary>
        /// Состояние
        /// </summary>
        [DataMember]
        public int job_state { get; set; }

        /// <summary>
        /// Тип
        /// </summary>
        [DataMember]
        public int job_type { get; set; }

        /// <summary>
        /// Код
        /// </summary>
        [DataMember]
        public string job_code { get; set; }

        /// <summary>
        /// Название
        /// </summary>
        [DataMember]
        public string job_name { get; set; }

        /// <summary>
        /// Входные данные
        /// </summary>
        [DataMember]
        public string data { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        [DataMember]
        public DateTime create_date { get; set; }

        /// <summary>
        /// Дата начала обработки
        /// </summary>
        [DataMember]
        public DateTime? start_date { get; set; }

        /// <summary>
        /// Дата окончания обработки
        /// </summary>
        [DataMember]
        public DateTime? end_date { get; set; }

        /// <summary>
        /// Признак того, что задача выполняется
        /// </summary>
        [DataMember]
        public DateTime heart_beat { get; set; }

        /// <summary>
        /// Признак успешности обработки
        /// </summary>
        [DataMember]
        public bool success { get; set; }

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        [DataMember]
        public string message { get; set; }
    }





    [DataContract]
    public class FileDom
    {
        [DataMember]
        public decimal id { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
        [DataMember]
        public int ukds { get; set; }
        [DataMember]
        public string town { get; set; }
        [DataMember]
        public int nzp_town { get; set; }
        [DataMember]
        public string rajon { get; set; }
        [DataMember]
        public int nzp_raj { get; set; }
        [DataMember]
        public string ulica { get; set; }
        [DataMember]
        public string ndom { get; set; }
        [DataMember]
        public string nkor { get; set; }
        [DataMember]
        public decimal area_id { get; set; }
        [DataMember]
        public string cat_blago { get; set; }
        [DataMember]
        public int etazh { get; set; }
        [DataMember]
        public string build_year { get; set; }
        [DataMember]
        public decimal total_square { get; set; }
        [DataMember]
        public decimal mop_square { get; set; }
        [DataMember]
        public decimal useful_square { get; set; }
        [DataMember]
        public decimal mo_id { get; set; }
        [DataMember]
        public string params_ { get; set; }
        [DataMember]
        public int ls_row_number { get; set; }
        [DataMember]
        public int odpu_row_number { get; set; }
        [DataMember]
        public int nzp_ul { get; set; }
        [DataMember]
        public int nzp_dom { get; set; }
        [DataMember]
        public string comment { get; set; }

        public FileDom()
            : base()
        {
            id = 0;
            nzp_file = 0;
            nzp_town = 0;
            nzp_raj = 0;
            ukds = 0;
            town = "";
            rajon = "";
            ulica = "";
            ndom = "";
            nkor = "";
            area_id = 0;
            cat_blago = "";
            etazh = 0;
            build_year = "";
            total_square = 0;
            mop_square = 0;
            useful_square = 0;
            mo_id = 0;
            params_ = "";
            ls_row_number = 0;
            odpu_row_number = 0;
            nzp_ul = 0;
            nzp_dom = 0;
            comment = "";
        }
    }

    [DataContract]
    public class FileKvar
    {
        [DataMember]
        public decimal id { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
        [DataMember]
        public int ukas { get; set; }
        [DataMember]
        public decimal dom_id { get; set; }
        [DataMember]
        public int ls_type { get; set; }
        [DataMember]
        public string fam { get; set; }
        [DataMember]
        public string ima { get; set; }
        [DataMember]
        public string otch { get; set; }
        [DataMember]
        public string birth_date { get; set; }
        [DataMember]
        public string nkvar { get; set; }
        [DataMember]
        public string nkvar_n { get; set; }
        [DataMember]
        public string open_date { get; set; }
        [DataMember]
        public string opening_osnov { get; set; }
        [DataMember]
        public string close_date { get; set; }
        [DataMember]
        public string closing_osnov { get; set; }
        [DataMember]
        public int kol_gil { get; set; }
        [DataMember]
        public int kol_vrem_prib { get; set; }
        [DataMember]
        public int kol_vrem_ub { get; set; }
        [DataMember]
        public int room_number { get; set; }
        [DataMember]
        public decimal total_square { get; set; }
        [DataMember]
        public decimal living_square { get; set; }
        [DataMember]
        public decimal otapl_square { get; set; }
        [DataMember]
        public decimal naim_square { get; set; }
        [DataMember]
        public int is_communal { get; set; }
        [DataMember]
        public int is_el_plita { get; set; }
        [DataMember]
        public int is_gas_plita { get; set; }
        [DataMember]
        public int is_gas_colonka { get; set; }
        [DataMember]
        public int is_fire_plita { get; set; }
        [DataMember]
        public int gas_type { get; set; }
        [DataMember]
        public int water_type { get; set; }
        [DataMember]
        public int hotwater_type { get; set; }
        [DataMember]
        public int canalization_type { get; set; }
        [DataMember]
        public int is_open_otopl { get; set; }
        [DataMember]
        public string params_ { get; set; }
        [DataMember]
        public int service_row_number { get; set; }
        [DataMember]
        public int reval_params_row_number { get; set; }
        [DataMember]
        public int ipu_row_number { get; set; }
        [DataMember]
        public int nzp_dom { get; set; }
        [DataMember]
        public int nzp_kvar { get; set; }
        [DataMember]
        public string comment { get; set; }

        public FileKvar()
            : base()
        {
            id = 0;
            nzp_file = 0;
            ukas = 0;
            dom_id = 0;
            ls_type = 0;
            fam = "";
            ima = "";
            otch = "";
            birth_date = "";
            nkvar = "";

            nkvar_n = "";
            open_date = "";
            opening_osnov = "";
            close_date = "";
            closing_osnov = "";
            kol_gil = 0;
            kol_vrem_prib = 0;
            kol_vrem_ub = 0;
            room_number = 0;
            total_square = 0;

            living_square = 0;
            otapl_square = 0;
            naim_square = 0;
            is_communal = 0;
            is_el_plita = 0;
            is_gas_plita = 0;
            is_gas_colonka = 0;
            is_fire_plita = 0;
            gas_type = 0;
            water_type = 0;

            hotwater_type = 0;
            canalization_type = 0;
            is_open_otopl = 0;
            params_ = "";
            service_row_number = 0;
            reval_params_row_number = 0;
            ipu_row_number = 0;
            nzp_dom = 0;
            nzp_kvar = 0;
            comment = "";
        }
    }

    [DataContract]
    public class FileSupp
    {
        [DataMember]
        public decimal id { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
        [DataMember]
        public decimal supp_id { get; set; }
        [DataMember]
        public string supp_name { get; set; }
        [DataMember]
        public string jur_address { get; set; }
        [DataMember]
        public string fact_address { get; set; }
        [DataMember]
        public string inn { get; set; }
        [DataMember]
        public string kpp { get; set; }
        [DataMember]
        public string rs { get; set; }
        [DataMember]
        public string bank { get; set; }
        [DataMember]
        public string bik { get; set; }
        [DataMember]
        public string ks { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }

        public FileSupp()
            : base()
        {
            id = 0;
            nzp_file = 0;
            supp_id = 0;
            supp_name = "";
            jur_address = "";
            fact_address = "";
            inn = "";
            kpp = "";
            rs = "";
            bank = "";
            bik = "";
            ks = "";
            nzp_supp = 0;
        }
    }

    [DataContract]
    public class FileIPU
    {
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
        [DataMember]
        public string ls_id { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public int rashod_type { get; set; }
        [DataMember]
        public int serv_type { get; set; }
        [DataMember]
        public string counter_type { get; set; }
        [DataMember]
        public int cnt_stage { get; set; }
        [DataMember]
        public int mmnog { get; set; }
        [DataMember]
        public string num_cnt { get; set; }
        [DataMember]
        public string dat_uchet { get; set; }
        [DataMember]
        public float val_cnt { get; set; }
        [DataMember]
        public int nzp_measure { get; set; }
        [DataMember]
        public string dat_prov { get; set; }
        [DataMember]
        public string dat_provnext { get; set; }
        [DataMember]
        public int nzp_kvar { get; set; }
        [DataMember]
        public int nzp_counter { get; set; }

        public FileIPU()
            : base()
        {
            id = 0;
            nzp_file = 0;
            ls_id = "";
            nzp_serv = 0;
            rashod_type = 0;
            serv_type = 0;
            counter_type = "";
            cnt_stage = 0;
            mmnog = 0;
            num_cnt = "";
            dat_uchet = "";
            val_cnt = 0;
            nzp_measure = 0;
            dat_prov = "";
            dat_provnext = "";
            nzp_kvar = 0;
            nzp_counter = 0;
        }


    }

    [DataContract]
    public class TransferHome : Finder
    {
        public TransferHome()
            : base()
        {
        }

        [DataMember]
        public string address { get; set; }

        [DataMember]
        public int nzp_dom { get; set; }

        [DataMember]
        public int nzp_wp { get; set; }

        [DataMember]
        public int limit { get; set; }

        [DataMember]
        public int offset { get; set; }
    }

}

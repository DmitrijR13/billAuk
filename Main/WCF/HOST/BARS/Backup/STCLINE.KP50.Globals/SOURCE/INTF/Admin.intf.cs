using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using STCLINE.KP50.Global;
using System.Net.Mail;
using System.Runtime.Serialization;

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
        Returns LoadHarGilFondGKU(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<KLADRData>> LoadDataFromKLADR(KLADRFinder finder);

        [OperationContract]
        Returns RefreshKLADRFile(FilesImported finder);

        [OperationContract]
        Returns UploadUESCharge(FilesImported finder);

        [OperationContract]
        Returns UploadMURCPayment(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedAreas>> GetComparedArea(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedSupps>> GetComparedSupp(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedVills>> GetComparedMO(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedServs>> GetComparedServ(Finder finder);
        
        [OperationContract]
        ReturnsObjectType<List<ComparedMeasures>> GetComparedMeasure(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedParTypes>> GetComparedParType(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedParTypes>> GetComparedParBlag(Finder finder);

        [OperationContract]
        Returns ReadReestrFromCbb(FilesImported finderpack, FilesImported finder, string connectionString);

        [OperationContract]
        ReturnsObjectType<List<ComparedParTypes>> GetComparedParGas(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedParTypes>> GetComparedParWater(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedTowns>> GetComparedTown(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedRajons>> GetComparedRajon(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedStreets>> GetComparedStreets(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedHouses>> GetComparedHouse(Finder finder);

        [OperationContract]
        Returns SetToChange(ServFormulFinder finder);

        [OperationContract]
        ReturnsObjectType<List<ServFormulFinder>> GetServFormul(Finder finder);



        [OperationContract]
        ReturnsObjectType<List<UncomparedAreas>> GetUncomparedArea(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedSupps>> GetUncomparedSupp(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedVills>> GetUncomparedMO(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedServs>> GetUncomparedServ(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParType(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParBlag(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParGas(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParWater(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedMeasures>> GetUncomparedMeasure(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedTowns>> GetUncomparedTown(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedRajons>> GetUncomparedRajon(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedStreets>> GetUncomparedStreets(Finder finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedHouses>> GetUncomparedHouse(Finder finder);

        [OperationContract]
        ReturnsType UnlinkArea(ComparedAreas finder);

        [OperationContract]
        ReturnsType UnlinkSupp(ComparedSupps finder);

        [OperationContract]
        ReturnsType UnlinkMO(ComparedVills finder);

        [OperationContract]
        ReturnsType UnlinkServ(ComparedServs finder);

        [OperationContract]
        ReturnsType UnlinkParType(ComparedParTypes finder);

        [OperationContract]
        ReturnsType UnlinkParBlag(ComparedParTypes finder);

        [OperationContract]
        ReturnsType UnlinkParGas(ComparedParTypes finder);

        [OperationContract]
        ReturnsType UnlinkParWater(ComparedParTypes finder);

        [OperationContract]
        ReturnsType UnlinkMeasure(ComparedMeasures finder);

        [OperationContract]
        ReturnsType UnlinkTown(ComparedTowns finder);

        [OperationContract]
        ReturnsType UnlinkRajon(ComparedRajons finder);

        [OperationContract]
        ReturnsType UnlinkStreet(ComparedStreets finder);

        [OperationContract]
        ReturnsType UnlinkHouse(ComparedHouses finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedAreas>> GetAreaByFilter(UncomparedAreas finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedSupps>> GetSuppByFilter(UncomparedSupps finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedVills>> GetMOByFilter(UncomparedVills finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedServs>> GetServByFilter(UncomparedServs finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetParTypeByFilter(UncomparedParTypes finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetParBlagByFilter(UncomparedParTypes finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetParGasByFilter(UncomparedParTypes finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetParWaterByFilter(UncomparedParTypes finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedMeasures>> GetMeasureByFilter(UncomparedMeasures finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedTowns>> GetTownByFilter(UncomparedTowns finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedRajons>> GetRajonByFilter(UncomparedRajons finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedStreets>> GetStreetsByFilter(UncomparedStreets finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedHouses>> GetHouseByFilter(UncomparedHouses finder);

        [OperationContract]
        ReturnsType LinkArea(UncomparedAreas finder);

        [OperationContract]
        ReturnsType LinkSupp(UncomparedSupps finder);

        [OperationContract]
        ReturnsType LinkMO(UncomparedVills finder);

        [OperationContract]
        ReturnsType LinkServ(UncomparedServs finder);

        [OperationContract]
        ReturnsType LinkParType(UncomparedParTypes finder);

        [OperationContract]
        ReturnsType LinkParBlag(UncomparedParTypes finder);

        [OperationContract]
        ReturnsType LinkParGas(UncomparedParTypes finder);

        [OperationContract]
        ReturnsType LinkParWater(UncomparedParTypes finder);

        [OperationContract]
        ReturnsType LinkMeasure(UncomparedMeasures finder);

        [OperationContract]
        ReturnsType LinkTown(UncomparedTowns finder);

        [OperationContract]
        ReturnsType LinkRajon(UncomparedRajons finder);

        [OperationContract]
        ReturnsType LinkNzpStreet(UncomparedStreets finder);

        [OperationContract]
        ReturnsType LinkNzpDom(UncomparedHouses finder);

        [OperationContract]
        ReturnsType AddNewArea(UncomparedAreas finder);

        [OperationContract]
        ReturnsType AddNewSupp(UncomparedSupps finder);

        [OperationContract]
        ReturnsType AddNewMO(UncomparedVills finder);

        [OperationContract]
        ReturnsType AddNewServ(UncomparedServs finder);

        [OperationContract]
        ReturnsType AddNewParType(UncomparedParTypes finder);

        [OperationContract]
        ReturnsType AddNewParBlag(UncomparedParTypes finder);

        [OperationContract]
        ReturnsType AddNewParGas(UncomparedParTypes finder);

        [OperationContract]
        ReturnsType AddNewParWater(UncomparedParTypes finder);

        [OperationContract]
        ReturnsType AddNewMeasure(UncomparedMeasures finder);

        [OperationContract]
        ReturnsType AddNewStreet(UncomparedStreets finder);

        [OperationContract]
        ReturnsType AddNewHouse(UncomparedHouses finder);

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

        [OperationContract]
        Returns AddAllHouse(FilesImported finder);

        [OperationContract]
        Returns DeleteUnrelatedInfo();

        [OperationContract]
        Returns DeleteFromExchangeSZ(Finder finder, int nzp_ex_sz);

        [OperationContract]
        ReturnsType DbSaveFileToDisassembly(int nzp_file);

        [OperationContract]
        ReturnsObjectType<List<UploadingData>> GetUploadingProgress(UploadingData finder);

        [OperationContract]
        ReturnsObjectType<List<FilesImported>> GetFiles(FilesImported finder);

        /// <summary>
        /// Операции с загруженными файлами
        /// </summary>
        /// <param name="finder">файл</param>
        /// <param name="operation">операция</param>
        /// <returns>результат</returns>
        [OperationContract]
        Returns OperateWithFileImported(FilesImported finder, FilesImportedOperations operation);

        [OperationContract]
        Returns UploadInDb(FilesImported finder, UploadOperations operation, UploadMode mode);

        [OperationContract]
        Returns UploadGilec(List<int> lst);

        [OperationContract]
        Returns GetUploadExchangeSZ(Finder finder, string file_name);
    }

    public enum FilesImportedOperations
    {
        Delete,
        Disassemble
    }

    public enum UploadOperations
    {
        Area,
        Dom,
        Kvar,
        Supp
    }

    public enum UploadMode
    {
        Add,
        Update
    }

    [DataContract]
    public class Role : Finder
    {
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public string role { get; set; }
        [DataMember]
        public int page_url { get; set; }
        [DataMember]
        public string page_name { get; set; }
        [DataMember]
        public int sort { get; set; }
        [DataMember]
        public int nzpuser { get; set; }
        [DataMember]
        public int is_active { get; set; }

        public Role()
            : base()
        {
            num = 0;
            nzp_role = 0;
            role = "";
            page_url = 0;
            page_name = "";
            sort = 0;
            nzpuser = 0;
            is_active = 1;
        }

    }

    [DataContract]
    public class UserAccess : Finder
    {
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public int nzp_lacc { get; set; }
        [DataMember]
        public int nzpuser { get; set; }
        [DataMember]
        public int acc_kod { get; set; }
        [DataMember]
        public string accName { 
            get 
            {
                switch (acc_kod)
                {
                    case 1: return "Вход в систему";
                    case 2: return "Завершение работы";
                    default: return "";
                }
            }
            set { }
        }
        [DataMember]
        public string dat_log { get; set; }
        [DataMember]
        public string dat_log_po { get; set; }
        [DataMember]
        public string ip_log { get; set; }
        [DataMember]
        public string browser { get; set; }
        [DataMember]
        public string login { get; set; }
        [DataMember]
        public string pwd { get; set; }
        [DataMember]
        public string idses { get; set; }
        [DataMember]
        public string dat_exit { get; set; }

        public UserAccess()
            : base()
        {
            num = 0;
            nzp_lacc = 0;
            nzpuser = 0;
            acc_kod = 0;
            dat_log = "";
            dat_log_po = "";
            ip_log = "";
            browser = "";
            login = "";
            pwd = "";
            idses = "";
            dat_exit = "";
        }
    }

    [DataContract]
    public class BaseUser: Finder //данные пользователя
    {
        public BaseUser()
            : base()
        {
            login = "";
            password = "";
            info = "";
            uname = "";
            dat_log = "";
            idses = "";
            ip_log = "";
            browser = "";
            nzp_payer = 0;
            payer = "";
            nzp_area = 0;
            nzp_supp = 0;
            nzp_disp = 0;
            sessionId = "";
        }

        [DataMember]
        public string login { get; set; }
        [DataMember]
        public string password { get; set; }
        [DataMember]
        public string info { get; set; }
        [DataMember]
        public string uname { get; set; }
        [DataMember]
        public string dat_log { get; set; }
        [DataMember]
        public string idses { get; set; }
        [DataMember]
        public string ip_log { get; set; }
        [DataMember]
        public string browser { get; set; }
        [DataMember]
        public string sessionId { get; set; }

        /// <summary>
        /// Тип организации
        /// </summary>
        public OrganizationTypes organizationType
        {
            get
            {
                if (nzp_payer > 0)
                {
                    if (nzp_payer == Payers.DispatchingOffice.GetHashCode()) return OrganizationTypes.DispatchingOffice;
                    else if (nzp_area > 0) return OrganizationTypes.UK;
                    else if (nzp_supp > 0) return OrganizationTypes.Supplier;
                    else return OrganizationTypes.Other;
                }
                else return OrganizationTypes.None;
            }
        }

        /// <summary>
        /// Типы организаций, к которым принадлежат пользователи
        /// </summary>
        public enum OrganizationTypes
        {
            /// <summary>
            /// Организация не задана
            /// </summary>
            None = 0,

            /// <summary>
            /// Диспетчерская
            /// </summary>
            DispatchingOffice = 1,

            /// <summary>
            /// Управляющая компания (Территория)
            /// </summary>
            UK = 2,

            /// <summary>
            /// Поставщик услуг
            /// </summary>
            Supplier = 3,

            /// <summary>
            /// Прочие
            /// </summary>
            Other = 4
        }

        /// <summary>
        /// Код организации (подрядчик), к которой привязан пользователь
        /// </summary>
        [DataMember]
        public int nzp_payer { get; set; }

        /// <summary>
        /// Наименование организации, к которой привязан пользователь
        /// </summary>
        [DataMember]
        public string payer { get; set; }

        /// <summary>
        /// Код территории
        /// </summary>
        [DataMember]
        public int nzp_area { get; set; }

        /// <summary>
        /// Код поставщика
        /// </summary>
        [DataMember]
        public int nzp_supp { get; set; }

        /// <summary>
        /// Код диспетчерской
        /// </summary>
        [DataMember]
        public int nzp_disp { get; set; }
    }

    [DataContract]
    public class User : BaseUser
    {
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public int nzpuser { get; set; }
        [DataMember]
        public string pwd { get; set; }
        [DataMember]
        public string email { get; set; }
        [DataMember]
        public byte is_blocked { get; set; }
        [DataMember]
        public byte is_remote { get; set; }
        [DataMember]
        public string blockName { get { if (is_blocked == 1) return "Заблокирован"; else return ""; } set { } }
        [DataMember]
        public int rolesNumber { get; set; }
        [DataMember]
        public List<Role> roles { get; set; }
        [DataMember]
        public int accessNumber { get; set; }
        [DataMember]
        public List<UserAccess> access { get; set; }
        [DataMember]
        public Role roleFinder { get; set; }
        [DataMember]
        public UserAccess accessFinder { get; set; }
        [DataMember]
        public string appointment { get; set; }
        [DataMember]
        public int nzp_bank { get; set; }
        [DataMember]
        public string bank { get; set; }

        /// <summary>
        /// признак, что пользователь на сайте
        /// > 0 - на сайте
        /// = 0 - не на сайте
        /// < 0 - не важно (применяется как фильтр для поиска)
        /// </summary>
        [DataMember]
        public int isOnline { get; set; }
        [DataMember]
        public string isOnlineText
        {
            get { if (isOnline > 0) return "На сайте"; else return ""; }
            set { }
        }

        /// <summary>
        /// уникальный идентификатор запроса на восстановление пароля
        /// </summary>
        [DataMember]
        public string requestId { get; set; }

        public User()
            : base()
        {
            num = 0;
            nzpuser = 0;
            pwd = "";
            uname = "";
            email = "";
            is_blocked = 0;
            is_remote = 0;
            rolesNumber = 0;
            roles = new List<Role>();
            accessNumber = 0;
            access = new List<UserAccess>();
            roleFinder = null;
            accessFinder = null;
            isOnline = 0;
            requestId = "";
            appointment = "";
            nzp_bank = 0;
            bank = "";
        }

    }

    public enum ProcessTypes
    { 
        None = 0x00,
        CalcSaldoUK = 0x01,
        CalcNach = 0x02,
        Bill = 0x03,
        PayDoc=0x05
    }

    [DataContract]
    public class BackgroundProcess : Finder
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
            InQueue = 3,

            /// <summary>
            /// В процессе выполнения
            /// </summary>
            InProcess = 0,

            /// <summary>
            /// Успешно выполнено
            /// </summary>
            Success = 2,

            /// <summary>
            /// Не выполнено
            /// </summary>
            Failed = -1,
        }

        public static Statuses GetStatusById(int id)
        {
            if (id == (int)Statuses.InQueue) return Statuses.InQueue;
            else if (id == (int)Statuses.InProcess) return Statuses.InProcess;
            else if (id == (int)Statuses.Success) return Statuses.Success;
            else if (id == (int)Statuses.Failed) return Statuses.Failed;
            else return Statuses.None;
        }

        public static string GetStatusName(Statuses status)
        {
            switch (status)
            {
                case Statuses.InQueue: return "В очереди";
                case Statuses.InProcess: return "Выполняется";
                case Statuses.Success: return "Выполнено";
                case Statuses.Failed: return "Ошибка";
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
            kod_info = (int)status;
        }

        [DataMember]
        public int num { get; set; }
        [DataMember]
        public int nzp_key { get; set; }
        [DataMember]
        public int kod_info { get; set; }
        
        [DataMember]
        public virtual string status
        {
            get
            {
                return GetStatusNameById(kod_info);
            }
            set { }
        }

        static public int getKodInfo(int nzpAct)
        { 
            switch(nzpAct)
            {
                case Constants.act_process_active: return 0;
                case Constants.act_process_finished: return 2;
                case Constants.act_process_in_queue: return 3;
                case Constants.act_process_with_errors: return -1;
                default: return Constants._ZERO_;
            }
        }

        [DataMember]
        public string dat_in { get; set; }
        [DataMember]
        public string dat_in_po { get; set; }
        [DataMember]
        public string dat_work { get; set; }
        [DataMember]
        public string dat_out { get; set; }
        [DataMember]
        public string txt { get; set; }
        public string prms { get; set; }

        [DataMember]
        public string processName { get { return "Фоновый процесс от " + dat_in; } }
        [DataMember]
        public string processType { get { return "Фоновый процесс"; } }
        [DataMember]
        public ProcessTypes processTypeID { get { return ProcessTypes.None; } }
        [DataMember]
        public decimal progress { get; set; }

        public BackgroundProcess()
            : base()
        {
            num = 0;
            nzp_key = 0;
            kod_info = Constants._ZERO_;
            dat_in = "";
            dat_in_po = "";
            dat_work = "";
            dat_out = "";
            txt = "";
            prms = "";
            progress = 0;
        }
    }

    [DataContract]
    public class ProcessWithYearMonth : BackgroundProcess
    {
        public RecordMonth YM, YM_po;

        [DataMember]
        public int month_
        {
            get { return YM.month_; }
            set { YM.month_ = value; }
        }
        [DataMember]
        public int year_
        {
            get { return YM.year_; }
            set { YM.year_ = value; }
        }

        [DataMember]
        public string year_month
        {
            get { return YM.name; }
        }

        [DataMember]
        public int month_po
        {
            get { return YM_po.month_; }
            set { YM_po.month_ = value; }
        }
        [DataMember]
        public int year_po
        {
            get { return YM_po.year_; }
            set { YM_po.year_ = value; }
        }
        public ProcessWithYearMonth()
            : base()
        {
            YM = new RecordMonth();
            YM.month_ = 0;
            YM.year_ = 0;

            YM_po = new RecordMonth();
            YM_po.month_ = 0;
            YM_po.year_ = 0;
        }
    }

    [DataContract]
    public class ProcessSaldo : ProcessWithYearMonth
    {
        [DataMember]
        public int nzp_area { get; set; }
        [DataMember]
        public string area { get; set; }

        [DataMember]
        public new string processType { get { return "Расчет сальдо УК"; } }

        [DataMember]
        public new ProcessTypes processTypeID { get { return ProcessTypes.CalcSaldoUK; } }

        [DataMember]
        public new string processName
        {
            get { return processType + " для " + area + " за " + year_month + " от " + dat_in; }
        }

        public ProcessSaldo()
            : base()
        {
            nzp_area = 0;
            area = "";
        }
    }

    [DataContract]
    public class ProcessCalc : ProcessWithYearMonth
    {
        /// <summary> Код дома или 0 - полный расчет
        /// </summary>
        [DataMember]
        public int nzp { get; set; }
        
        [DataMember]
        public int nzpt { get; set; }
        
        /// <summary> Разновидность расчета (0 - полный)
        /// </summary>
        [DataMember]
        public int task { get; set; }
        
        /// <summary> Приоритет
        /// </summary>
        [DataMember]
        public int prior { get; set; }
        
        /// <summary> Номер очереди заданий (соответствует номеру таблицы calc_fon_0, calc_fon_1, ...)
        /// </summary>
        [DataMember]
        public int queue { get; set; }

        [DataMember]
        public new string processType { get { return "Расчет начислений"; } }

        [DataMember]
        public new ProcessTypes processTypeID { get { return ProcessTypes.CalcNach; } }

        [DataMember]
        public new string processName
        {
            get
            {
                string name = processType;
                if (nzp > 0) name += " дома";
                name += " за " + year_month + " от " + dat_in;
                return name;
            } 
        }

        public ProcessCalc()
            : base()
        {
            nzp = Constants._ZERO_;
            nzpt = 0;
            task = Constants._ZERO_;
            prior = 0;
            queue = Constants._ZERO_;
        }
    }

    [DataContract]
    public class ProcessBill : ProcessWithYearMonth
    {
        [DataMember]
        public int nzp_area { get; set; } 
        [DataMember]
        public string area { get; set; }  
        [DataMember]
        public int nzp_geu { get; set; }  
        [DataMember] 
        public string geu { get; set; }
        [DataMember]
        public int count_list_in_pack { get; set; }
        [DataMember]
        public int kod_sum_faktura { get; set; }
        [DataMember]
        public string result_file_type { get; set; }
        [DataMember]
        public int id_faktura { get; set; }
        [DataMember]
        public bool with_dolg { get; set; }

        [DataMember]
        public string file_name { get; set; }

        [DataMember]
        public new string processType { get { return "Формирование платежных документов"; } }

        [DataMember]
        public new ProcessTypes processTypeID { get { return ProcessTypes.Bill; } }

        [DataMember]
        public new string processName
        {
            get { return processType + " для: территория: " + area + ", локальный банк: " + point  + ", отделение:" + geu + " за " + year_month + " от " + dat_in; }
        }

        [DataMember]
        public override string status
        {
            get
            {
                Statuses stat = GetStatusById(kod_info);

                if (stat == Statuses.InProcess || stat == Statuses.Failed)
                    return GetStatusName(stat) + " (" + progress.ToString("P") + ")";
                else return GetStatusName(stat);
            }
            set { }
        }

        public ProcessBill()
            : base()
        {
            nzp_area = 0;
            area = "";
            nzp_geu = 0;
            geu = "";
            nzp_wp = 0;
            point = "";
            count_list_in_pack = 0;
            kod_sum_faktura = 0;
            result_file_type = "";
            id_faktura = 0;
            with_dolg = false;
            file_name = "";
        }

        public ProcessBill(int id, decimal prgrss)
            : this()
        {
            nzp_key = id;
            progress = prgrss;
        }
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
    public class ComparedAreas : Finder
    {
        [DataMember]
        public string area_file { get; set; }
        [DataMember]
        public string area_base { get; set; }
        [DataMember]
        public string nzp_area { get; set; }
    }

    [DataContract]
    public class ComparedSupps : Finder
    {
        [DataMember]
        public string supp_file { get; set; }
        [DataMember]
        public string supp_base { get; set; }
        [DataMember]
        public string nzp_supp { get; set; }
    }

    [DataContract]
    public class ComparedVills : Finder
    {
        [DataMember]
        public string vill_file { get; set; }
        [DataMember]
        public string vill_base { get; set; }
        [DataMember]
        public string nzp_vill { get; set; }
    }

    [DataContract]
    public class ComparedServs : Finder
    {
        [DataMember]
        public string serv_file { get; set; }
        [DataMember]
        public string serv_base { get; set; }
        [DataMember]
        public string nzp_serv { get; set; }
    }

    [DataContract]
    public class ComparedParTypes : Finder
    {
        [DataMember]
        public string name_prm_file { get; set; }
        [DataMember]
        public string name_prm_base { get; set; }
        [DataMember]
        public string nzp_prm { get; set; }
        [DataMember]
        public string name_prm { get; set; }
    }

    [DataContract]
    public class ComparedMeasures : Finder
    {
        [DataMember]
        public string measure_file { get; set; }
        [DataMember]
        public string measure_base { get; set; }
        [DataMember]
        public string nzp_measure { get; set; }
    }

    [DataContract]
    public class ComparedTowns : Finder
    {
        [DataMember]
        public string town_file { get; set; }
        [DataMember]
        public string town_base { get; set; }
        [DataMember]
        public string nzp_town { get; set; }
    }

    [DataContract]
    public class ComparedRajons : Finder
    {
        [DataMember]
        public string rajon_file { get; set; }
        [DataMember]
        public string rajon_base { get; set; }
        [DataMember]
        public string nzp_raj { get; set; }
    }

    [DataContract]
    public class ComparedStreets : Finder
    {
        [DataMember]
        public string ulica_file { get; set; }
        [DataMember]
        public string ulica_base { get; set; }
        [DataMember]
        public string nzp_ul { get; set; }
    }

    [DataContract]
    public class ComparedHouses : Finder
    {
        [DataMember]
        public string dom_file { get; set; }
        [DataMember]
        public string dom_base { get; set; }
        [DataMember]
        public string nzp_dom { get; set; }
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
    public class UncomparedAreas : Finder
    {
        [DataMember]
        public string area { get; set; }
        [DataMember]
        public string nzp_area { get; set; }
    }

    [DataContract]
    public class UncomparedSupps : Finder
    {
        [DataMember]
        public string supp { get; set; }
        [DataMember]
        public string nzp_supp { get; set; }
    }

    [DataContract]
    public class UncomparedVills : Finder
    {
        [DataMember]
        public string vill { get; set; }
        [DataMember]
        public string nzp_vill { get; set; }
    }

    [DataContract]
    public class UncomparedServs : Finder
    {
        [DataMember]
        public string serv { get; set; }
        [DataMember]
        public string nzp_serv { get; set; }
    }

    [DataContract]
    public class UncomparedParTypes : Finder
    {
        [DataMember]
        public string name_prm { get; set; }
        [DataMember]
        public string nzp_prm { get; set; }
        [DataMember]
        public string type_prm { get; set; }
    }

    [DataContract]
    public class UncomparedMeasures : Finder
    {
        [DataMember]
        public string measure { get; set; }
        [DataMember]
        public string nzp_measure { get; set; }
    }

    [DataContract]
    public class UncomparedTowns : Finder
    {
        [DataMember]
        public string town { get; set; }
        [DataMember]
        public string nzp_town { get; set; }
    }

    [DataContract]
    public class UncomparedRajons : Finder
    {
        [DataMember]
        public string show_data { get; set; }
        [DataMember]
        public string rajon { get; set; }
        [DataMember]
        public string nzp_town { get; set; }
        [DataMember]
        public string nzp_raj { get; set; }
    }

    [DataContract]
    public class UncomparedStreets : Finder
    {
        [DataMember]
        public string show_data { get; set; }
        [DataMember]
        public string ulica { get; set; }
        [DataMember]
        public string nzp_town { get; set; }
        [DataMember]
        public string nzp_raj { get; set; }
        [DataMember]
        public string nzp_ul { get; set; }
        [DataMember]
        public string id { get; set; }
    }

    [DataContract]
    public class UncomparedHouses : Finder
    {
        [DataMember]
        public string show_data { get; set; }
        [DataMember]
        public string dom { get; set; }
        [DataMember]
        public string nzp_town { get; set; }
        [DataMember]
        public string nzp_raj { get; set; }
        [DataMember]
        public string nzp_ul { get; set; }
        [DataMember]
        public string nzp_dom { get; set; }
        [DataMember]
        public string id { get; set; }
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
    public class KLADRFinder : Finder
    {
        [DataMember]
        public string query { get; set; }

        [DataMember]
        public bool loadStreets { get; set; }

        [DataMember]
        public bool clearAddrSpace { get; set; }

        [DataMember]
        public string level { get; set; }

        [DataMember]
        public KLADRData region { get; set; }
        [DataMember]
        public KLADRData district { get; set; }
        [DataMember]
        public KLADRData city { get; set; }
        [DataMember]
        public KLADRData settlement { get; set; }
        [DataMember]
        public KLADRData street { get; set; }
        
        [DataMember]
        public List<KLADRData> regionList { get; set; }
        [DataMember]
        public List<KLADRData> districtList { get; set; }
        [DataMember]
        public List<KLADRData> cityList { get; set; }
        [DataMember]
        public List<KLADRData> settlementList { get; set; }
        [DataMember]
        public List<KLADRData> streetList { get; set; }

        [DataMember]
        public string regionCode { get; set; }
        [DataMember]
        public string districtCode { get; set; }
        [DataMember]
        public string cityCode { get; set; }
        [DataMember]
        public string settlementCode { get; set; }
        [DataMember]
        public string streetCode { get; set; }
    }

    [DataContract]
    public class KLADRData
    {
        [DataMember]
        public string fullname { get; set; }
        [DataMember]
        public string code { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string socr { get; set; }
    }

    [DataContract]
    public class UploadingData : Finder
    {
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public string num { get; set; }
        [DataMember]
        public DateTime date_upload { get; set; }
        [DataMember]
        public string progress { get; set; }
        [DataMember]
        public string status { get; set; }
        [DataMember]
        public int upload_type { get; set; }
    }

    [DataContract]
    public class FilesImported: Finder
    {
        public enum Statuses
        { 
            /// <summary>
            /// В процессе загрузки
            /// </summary>
            Loading = 1,
            Loaded = 2,
            LoadedWithErrors = 3,

            /// <summary>
            /// В процессе учета
            /// </summary>
            Applying = 4,
            Applied = 5,
            AppliedWithErrors = 6,

            //Удален
            Deleted = 7
        }

        [DataMember]
        public int file_type { get; set; }

        [DataMember]
        public string log_name { get; set; }

        [DataMember]
        public int nzp_exc { get; set; }

        [DataMember]
        public string ex_path { get; set; }

        [DataMember]
        public int nzp_exc_log { get; set; }

        [DataMember]
        public string ex_path_log { get; set; }

        [DataMember]
        public string year { get; set; }
        
        [DataMember]
        public string month { get; set; }

        [DataMember]
        public bool show_link_errors { get; set; }

        [DataMember]
        public bool use_previous_links { get; set; }

        [DataMember]
        public bool use_local_num { get; set; }

        [DataMember]
        public bool delete_all_data { get; set; }

        [DataMember]
        public bool prev_month_saldo { get; set; }

        [DataMember]
        public string num { get; set; }

        [DataMember]
        public DateTime? date { get; set; }

        [DataMember]
        public string format_version { get; set; }

        [DataMember]
        public int nzp_status { get; set; }

        [DataMember]
        public string status { get; set; }

        [DataMember]
        public string loaded_string { get; set; }

        [DataMember]
        public int nzp_file { get; set; }

        [DataMember]
        public bool[] sections { get; set; }

        [DataMember]
        public string percent { get; set; }


        /// <summary>
        /// Ключ формата файла
        /// </summary>
        [DataMember]
        public int nzp_ff { set; get; }

        /// <summary>
        /// Имя формата файла
        /// </summary>
        [DataMember]
        public string format_name { set; get; }

        /// <summary>
        /// Имя сохраненного файла
        /// </summary>
        [DataMember]
        public string saved_name { set; get; }

        /// <summary>
        /// Лог ошибок
        /// </summary>
        [DataMember]
        public string saved_name_log { set; get; }

        /// <summary>
        /// Исходное имя файла, который загружал пользователь
        /// </summary>
        [DataMember]
        public string loaded_name { set; get; }

        [DataMember]
        public bool to_disassembly { get; set; }


        public FilesImported()
            : base()
        {           
            nzp_file = 0;
            saved_name = "";
            loaded_name = "";
        }
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
        public int  kol_vrem_ub { get; set; }
        [DataMember]
        public int  room_number { get; set; }
        [DataMember]
        public decimal  total_square { get; set; }
        [DataMember]
        public decimal  living_square { get; set; }
        [DataMember]
        public decimal  otapl_square { get; set; }
        [DataMember]
        public decimal  naim_square { get; set; }
        [DataMember]
        public int is_communal { get; set; }
        [DataMember]
        public int  is_el_plita { get; set; }
        [DataMember]
        public int  is_gas_plita { get; set; }
        [DataMember]
        public int  is_gas_colonka { get; set; }
        [DataMember]
        public int is_fire_plita { get; set; }
        [DataMember]
        public int  gas_type { get; set; }
        [DataMember]
        public int  water_type { get; set; }
        [DataMember]
        public int hotwater_type { get; set; }
        [DataMember]
        public int canalization_type { get; set; }
        [DataMember]
        public int  is_open_otopl { get; set; }
        [DataMember]
        public string  params_ { get; set; }
        [DataMember]
        public int service_row_number { get; set; }
        [DataMember]
        public int reval_params_row_number { get; set; }
        [DataMember]
        public int ipu_row_number{ get; set; }
        [DataMember]
        public int  nzp_dom { get; set; }
        [DataMember]
        public int  nzp_kvar { get; set; }
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
        public string supp_name  { get; set; }
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
}

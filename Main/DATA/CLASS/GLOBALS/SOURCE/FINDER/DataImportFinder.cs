using System;
using System.Collections.Generic;
using System.Runtime.Serialization;



namespace STCLINE.KP50.Global
{

    [DataContract]
    public class FilesImported : Finder
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

        public enum FileType
        {
            UnlHarGilGKU = 1,
            FromSz = 2,
            LoadHarGilGKU = 3,
            HouseParamLoad = 4,
            KvarParamLoad = 5,
            OneTime = 99,
        }

        /// <summary>
        /// Коды версий выгрузки. Берутся из поля nzp_act таблицы s_action
        /// </summary>
        public enum UnloadFormatVersion
        {
            Version2_1 = 898,
            Version2_2 = 899,
            Version3 = 886,
            VersionBaikalskVstkb = 900,
            VersionBaikalskSocProtect = 901,
            TagilSber = 902,
            VersionBaikalskSber = 903,
            IssrpR102 = 904,
            IssrpF102 = 905,
            Version4 = 921
        }

        public enum UploadFormat
        {
            Tula = 1,
            MariyEl = 2,
            BaikalskVstkb = 1001,
            BaikalskSocProtectL = 1002,
            BaikalskSocProtectS = 1003,
            TagilSber = 1004,
            BaikalskSber = 1005,
            IssrpF112 = 1006,
            Dbf = 3,
            TxtGeneral = 4
        }


        [DataMember]
        public int upload_format { get; set; }

        [DataMember]
        public int file_type { get; set; }

        [DataMember]
        public string log_name { get; set; }

        [DataMember]
        public int nzp_exc { get; set; }

        [DataMember]
        public int nzp_bank { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }

        [DataMember]
        public string ex_path { get; set; }

        [DataMember]
        public int nzp_exc_log { get; set; }

        [DataMember]
        public string ex_path_log { get; set; }

        [DataMember]
        public int nzp_exc_rep_load { get; set; }

        [DataMember]
        public string ex_path_rep_load { get; set; }

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
        public bool is_last_month { get; set; }

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
        public Dictionary<int, bool> selectedSections { get; set; }

        [DataMember]
        public string percent { get; set; }

        [DataMember]
        public bool repair { get; set; }

        [DataMember]
        public bool reloaded_file { get; set; }

        [DataMember]
        public int type_load { get; set; }

        [DataMember]
        public int count_rows { get; set; }

        [DataMember]
        public decimal sum { get; set; }


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
        /// Имя отчета - акт загрузки данных
        /// </summary>
        [DataMember]
        public string rep_load_name { set; get; }

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

        [DataMember]
        public string diss_status { get; set; }
        [DataMember]
        public List<int> ListNzpWp { get; set; }

        /// <summary> Список кодов управляющих компаний </summary>
        [DataMember]
        public List<int> ListNzpArea { get; set; }
        /// <summary> Список кодов управляющих компаний </summary>
        [DataMember]
        public SimpleLoadTypeFile SimpLdFileType { get; set; }

        [DataMember]
        public SimpleLoadPayOrIpuType LoadPayOrIpuType { get; set; }

        [DataMember]
        public int nzp_file_1 { get; set; }
        [DataMember]
        public int nzp_file_2 { get; set; }

        [DataMember]
        public List<int> selectedFiles { get; set; }

        public FilesImported()
            : base()
        {
            nzp_file = -1;
            nzp_bank = 0;
            saved_name = "";
            loaded_name = "";
            upload_format = (int)FilesImported.UploadFormat.Tula;
            ListNzpWp = new List<int>();
            ListNzpArea = new List<int>();
            SimpLdFileType= SimpleLoadTypeFile.None;
            LoadPayOrIpuType = SimpleLoadPayOrIpuType.Pay;

            selectedFiles = new List<int>();
        }


    }
}

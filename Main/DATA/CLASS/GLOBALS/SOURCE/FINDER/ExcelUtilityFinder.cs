using STCLINE.KP50.Interfaces;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace STCLINE.KP50.Global
{
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
            if (id == (int)Statuses.InProcess) return Statuses.InProcess;
            if (id == (int)Statuses.Success) return Statuses.Success;
            if (id == (int)Statuses.Failed) return Statuses.Failed;
            if (id == (int)Statuses.NotSupported) return Statuses.NotSupported;
            return Statuses.None;
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

       
        public string stats_name { get; set; }

       [DataMember]
        public string dat_in { get; set; }

        [DataMember]
        public string dat_start { get; set; }

        [DataMember]
        public string dat_out { get; set; }

        [DataMember]
        public string dat_out_po { get; set; }

        [DataMember]
        public int tip { get; set; }

        [DataMember]
        public string exc_path { get; set; }

        [DataMember]
        public string exec_comment { get; set; }
        [DataMember]
        public string rep_name { get; set; }

        [DataMember]
        public int nzp_exc { get; set; }

       [DataMember]
        public decimal progress { get; set; }

        [DataMember]
        public int is_shared { get; set; } //общедоступность: 0 - доступен только создавшему пользователю; 1- всем 

        [DataMember]
        public string file_name { get; set; } //имя скачиваемого файла 

        [DataMember]
        public int is_user_has_role_admin { get; set; } //пользователь имеет роль администратора

        public Statuses status
        {
            get { return GetStatusById(stats); }
            set { SetStatus(value); }
        }

    
        public string status_name
        {
            get
            {
                Statuses stat = status;
                if (stat == Statuses.InProcess || stat == Statuses.Failed)
                    return GetStatusName(stat) + " (" + progress.ToString("P") + ")";
                return GetStatusName(stat);
            }
            set { }
        }

        public ExcelUtility()
            : base()
        {
            prms = "";
            stats = -1;
            dat_in = "";
            dat_out_po = "";
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
            file_name = "";
            is_user_has_role_admin = 0;
        }
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

    [DataContract]
    public class FinderChangeArea : Finder
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

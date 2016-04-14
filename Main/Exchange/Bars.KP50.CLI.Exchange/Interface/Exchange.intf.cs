using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Data;
using STCLINE.KP50.Global;
using System.Collections;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Exchange
    {

        #region  Загрузка оплат от ВТБ24
        /// <summary>
        /// Загрузка оплат от ВТБ24
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns CheckVtb24(FilesImported finder);

        /// <summary>
        /// Получение списка загруженных оплат от ВТБ24
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        List<VTB24Info> GetReestrsVTB24(ExFinder finder, out Returns ret);

        /// <summary>
        /// Удаление оплат от ВТБ24
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns DeleteReestrVTB24(ExFinder finder);

        /// <summary>
        /// Возможность пачки на распределение
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        bool CanDistr(ExFinder finder, out Returns ret);
        #endregion

        #region Обмен с поставщиками

        /// <summary>
        /// Получение списка файлов синхронизации
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        List<IReestrExSuppSyncLs> GetReestrSyncLs(ExFinder finder, out Returns ret);

        /// <summary>
        /// Удаление записи реестра
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns DeleteReestrRow(ExFinder finder);

        /// <summary>
        /// Получение списка файлов изменения ЛС
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        List<IReestrExSuppChangeLs> GetReestrChangeLs(ExFinder finder, out Returns ret);



        #endregion

        #region Обмен с соцзащитой

        [OperationContract]
        Returns DeleteFromExchangeSZ(Finder finder, int nzp_ex_sz);
        #endregion

        #region Загрузка счетчиков
        [OperationContract]
        Returns LoadSimpleCounters(int nzpUser, string webLogin, string fileName, string userFileName);
        #endregion

        #region Выгрузка в ЦХД
        [OperationContract]
        Returns UnloadToDw(int nzpUser, int year, int month, string pref, List<int> listNzpArea);
        #endregion

        #region Выгрузка в капремонт
        [OperationContract]
        Returns UnloadKapRem(int nzpUser, string year, string month, string pref);
        #endregion


        #region Загрузка начислений от сторонних поставщиков
        [OperationContract]
        Returns LoadSuppCharge(FilesImported finder);
        
        [OperationContract]
        List<string> GetServiceNameLoadSuppCharge(int nzpLoad);

        [OperationContract]
        Returns DisassemleLoadSuppCharge(int nzpLoad, Dictionary<string, int> services);

        [OperationContract]
        Returns DeleteSuppCharge(int nzpLoad);
        #endregion

        [OperationContract]
        List<SimpleLoadClass> GetSimpleLoadData(FilesImported finder, out Returns ret);

        [OperationContract]
        Returns Delete(SimpleLoadClass finder);
        [OperationContract]
        Returns LoadOplFromKass(FilesImported finder);
        [OperationContract]
        Returns LoadPayments(FilesImported finder);
        
        [OperationContract]
        Returns ImportParams(FilesImported finder);

        [OperationContract]
        List<PrmTypes> GetParamSprav(ParamCommon finder, out Returns ret);
        /// <summary>
        /// Проверка на существование загружаемого файла по его имени для загрузок SimpleLoad 
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns CheckSimpleLoadFileExixsts(FilesImported finder);
        /// <summary>
        /// Загрузка показаний счетчиков
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns LoadCountersReadings(FilesImported finder);
        /// <summary>
        /// Выгрузка показаний счетчиков
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns UnoadCountersReadings(FilesImported finder);
        /// <summary>
        /// Перемещает загрузки simple_load в архив
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns MoveLoadToArchive(SimpleLoadClass finder);
        /// <summary>
        /// Перемещает пачки из source_pack в архив
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
       [OperationContract]
        Returns MoveLoadedSourcePackToArchive(SimpleLoadClass finder);
    }

    public interface IExchangeRemoteObject : I_Exchange, IDisposable { }

    #region Прием оплат от ВТБ24
    public enum StatusVTB24
    {
        /// <summary>
        /// загружается
        /// </summary>
        None = 0,
        /// <summary>
        /// успешно загружен
        /// </summary>
        Success = 1,
        /// <summary>
        /// Ошибка при загрузке
        /// </summary>
        Fail = -1,

        /// <summary>
        /// Загружено, но сопоставлены не все ЛС
        /// </summary>
        WithErrors = 2,

        /// <summary>
        /// Распределено
        /// </summary>
        Distributed = 3,
        /// <summary>
        /// Ошибка при распределении
        /// </summary>
        DistFail = -2
    }



    [DataContract]
    public struct VTB24Info //реестр загрузок оплат для ВТБ24 
    {
        [DataMember]
        public int vtb24_down_id { get; set; }

        [DataMember]
        public DateTime message_date { get; set; }

        [DataMember]
        public DateTime download_date { get; set; }

        [DataMember]
        public decimal total_amount { get; set; }

        [DataMember]
        public decimal commission { get; set; }

        [DataMember]
        public int count_oper { get; set; }

        [DataMember]
        public string user_name { get; set; }

        [DataMember]
        public string file_name { get; set; }

        [DataMember]
        public string reciever { get; set; }

        [DataMember]
        public string sender { get; set; }

        [DataMember]
        public string status { get; set; }

        [DataMember]
        public int nzp_exc { get; set; }

        [DataMember]
        public string exc_path { get; set; }

        [DataMember]
        public string protocol { get; set; }

        [DataMember]
        public int nzp_status { get; set; }

        [DataMember]
        public int nzp_bank { get; set; }

        [DataMember]
        public string bank { get; set; }

        public string getNameStatusVTB24(int nzp_status)
        {
            switch (nzp_status)
            {
                case (int)StatusVTB24.None: return "Загружается";
                case (int)StatusVTB24.Success: return "Успешно";
                case (int)StatusVTB24.Fail: return "Ошибка";
                case (int)StatusVTB24.WithErrors: return "С ошибками";
                case (int)StatusVTB24.Distributed: return "Распределено";
                case (int)StatusVTB24.DistFail: return "Ошибка при распределении";

                default: return "Статус отсутствует";
            }
        }
    }


    [DataContract]
    public struct VTB24ReestrRow // оплаты для ВТБ24 
    {
        [DataMember]
        public int vtb24_id { get; set; }

        [DataMember]
        public int vtb24_down_id { get; set; }

        [DataMember]
        public string operation_uni { get; set; }

        [DataMember]
        public int number { get; set; }

        [DataMember]
        public DateTime date_operation { get; set; }

        [DataMember]
        public decimal account { get; set; }

        [DataMember]
        public decimal amount { get; set; }

        [DataMember]
        public decimal commission { get; set; }

        [DataMember]
        public decimal sum_money { get; set; }

    }



    [DataContract]
    public struct ProtocolVTB24
    {
        [DataMember]
        public DateTime message_date { get; set; }

        [DataMember]
        public DateTime download_date { get; set; }

        [DataMember]
        public decimal total_amount { get; set; }

        [DataMember]
        public decimal compared_amount { get; set; }

        [DataMember]
        public decimal commission { get; set; }

        [DataMember]
        public int count_oper { get; set; }

        [DataMember]
        public int compared_count { get; set; }

        [DataMember]
        public string user_name { get; set; }

        [DataMember]
        public string file_name { get; set; }

        [DataMember]
        public string receiver { get; set; }

        [DataMember]
        public string status { get; set; }

        [DataMember]
        public int file_id { get; set; }

        [DataMember]
        public string errors { get; set; }

        [DataMember]
        public List<VTB24ReestrRow> Rows;
    }

    #endregion

    #region Обмен со сторонними поставщиками



    /// <summary>
    /// реестр выгрузок оплат для поставщика
    /// </summary>
    [DataContract]
    public class ReestrExSuppPlat
    {
        [DataMember]
        public int nzp_reestr { get; set; }
        [DataMember]
        public string file_name { get; set; }
        //12ный код поставщика
        [DataMember]
        public string kod_supp { get; set; }
        //начало периода за который производится выгрузка
        [DataMember]
        public DateTime dat_s { get; set; }
        //конец периода
        [DataMember]
        public DateTime dat_po { get; set; }
        //номер версии
        [DataMember]
        public int version { get; set; }
        //дата выгрузки
        [DataMember]
        public DateTime date_unload { get; set; }
        [DataMember]
        public string user_name { get; set; }
        [DataMember]
        public int nzp_user { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public int status { get; set; }
        [DataMember]
        public string status_name { get; set; }
        [DataMember]
        public string errors { get; set; }
        [DataMember]
        public int nzp_err { get; set; }
        //ссылка на скачивание выгрузки
        [DataMember]
        public string file_link { get; set; }
        //ссылка на файл протокола
        [DataMember]
        public string prot_link { get; set; }
        //ссылка на лог
        [DataMember]
        public int nzp_log { get; set; }
        [DataMember]
        public int count_rows { get; set; }
        // сумма оплат в файле 
        [DataMember]
        public decimal all_sum_plat { get; set; }
        //кол-во показаний ПУ в файле
        [DataMember]
        public int count_pu { get; set; }

    }

    /// <summary>
    ///  оплата по услуге для поставщика
    /// </summary>
    [DataContract]
    public class ExSuppPlat
    {
        //ссылка на реестр
        [DataMember]
        public int nzp_reestr { get; set; }
        //серийник оплаты
        [DataMember]
        public int nzp_ex_supp_plat { get; set; }
        //Платежный код
        [DataMember]
        public decimal pkod { get; set; }
        //адрес
        [DataMember]
        public string adr { get; set; }
        //уникальный номер услуги в системе
        [DataMember]
        public int nzp_serv { get; set; }
        //конец периода
        [DataMember]
        public DateTime dat_po { get; set; }
        //номер версии
        [DataMember]
        public int version { get; set; }
        //дата выгрузки
        [DataMember]
        public DateTime date_unload { get; set; }
        [DataMember]
        public string user_name { get; set; }
        [DataMember]
        public int nzp_user { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public int status { get; set; }
        [DataMember]
        public string status_name { get; set; }
        [DataMember]
        public string errors { get; set; }
        [DataMember]
        public int nzp_err { get; set; }
        //ссылка на скачивание выгрузки
        [DataMember]
        public string file_link { get; set; }
        //ссылка на файл протокола
        [DataMember]
        public string prot_link { get; set; }
        //ссылка на лог
        [DataMember]
        public int nzp_log { get; set; }
        [DataMember]
        public int count_rows { get; set; }
        // сумма оплат в файле 
        [DataMember]
        public decimal all_sum_plat { get; set; }
        //кол-во показаний ПУ в файле
        [DataMember]
        public int count_pu { get; set; }

    }


    /// <summary>
    ///  реестр выгрузок файлов синхронизации
    /// </summary>
    [DataContract]
    public class ReestrExSuppSyncLs
    {
        [DataMember]
        public int nzp_reestr { get; set; }
        [DataMember]
        public string file_name { get; set; }
        //12ный код поставщика
        [DataMember]
        public string kod_supp { get; set; }
        //месяц за который производится выгрузка
        [DataMember]
        public DateTime saldo_date { get; set; }
        //номер версии
        [DataMember]
        public int version { get; set; }
        //номер файла за месяц
        [DataMember]
        public int num_file { get; set; }
        //дата выгрузки
        [DataMember]
        public DateTime date_unload { get; set; }

        [DataMember]
        public int nzp_user { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }

        [DataMember]
        public int status { get; set; }

        [DataMember]
        public int nzp_err { get; set; }
        //ссылка на скачивание выгрузки
        [DataMember]
        public int nzp_file_link { get; set; }
        //ссылка на лог
        [DataMember]
        public int nzp_log { get; set; }
        [DataMember]
        public int count_rows { get; set; }

        public ReestrExSuppSyncLs()
        {
            nzp_reestr = -1;
            nzp_err = -1;
            nzp_log = -1;
        }

    }
    public class IReestrExSuppSyncLs : ReestrExSuppSyncLs
    {

        [DataMember]
        public string user_name { get; set; }
        [DataMember]
        public string status_name { get; set; }
        [DataMember]
        public string errors { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public string version_name { get; set; }
        [DataMember]
        public string file_link { get; set; }
        public void getNameStatusFileSync(int status)
        {
            switch (status)
            {
                case (int)StatusUnload.InProcess: status_name = "В процессе"; break;
                case (int)StatusUnload.Success: status_name = "Успешно"; break;
                case (int)StatusUnload.Fail: status_name = "Ошибка"; break;
                default: status_name = "Статус отсутствует"; break;
            }
        }
    }

    /// <summary>
    /// реестр выгрузок с изменениями параметров ЛС для поставщика
    /// </summary>
    [DataContract]
    public class ReestrExSuppChangeLs
    {
        [DataMember]
        public int nzp_reestr { get; set; }
        [DataMember]
        public string file_name { get; set; }
        // дата начиная с которой выгружаем изменения
        [DataMember]
        public DateTime dat_s { get; set; }
        //номер версии
        [DataMember]
        public int version { get; set; }
        //номер файла за месяц
        [DataMember]
        public int num_file { get; set; }
        //дата выгрузки
        [DataMember]
        public DateTime date_unload { get; set; }
        [DataMember]
        public int nzp_user { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        //12ный код поставщика
        [DataMember]
        public string kod_supp { get; set; }
        [DataMember]
        public int status { get; set; }
        [DataMember]
        public int nzp_err { get; set; }
        //ссылка на скачивание выгрузки
        [DataMember]
        public int nzp_file_link { get; set; }
        //ссылка на лог
        [DataMember]
        public int nzp_log { get; set; }
        [DataMember]
        public int count_rows { get; set; }


    }

    public class IReestrExSuppChangeLs : ReestrExSuppChangeLs
    {
        [DataMember]
        public string user_name { get; set; }
        [DataMember]
        public string status_name { get; set; }
        [DataMember]
        public string errors { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public string version_name { get; set; }
        [DataMember]
        public string file_link { get; set; }

        public void getNameStatusChangeLs(int status)
        {
            switch (status)
            {
                case (int)StatusUnload.InProcess: status_name = "В процессе"; break;
                case (int)StatusUnload.Success: status_name = "Успешно"; break;
                case (int)StatusUnload.Fail: status_name = "Ошибка"; break;
                default: status_name = "Статус отсутствует"; break;
            }
        }
    }

    /// <summary>
    /// список версий с списком изменений
    /// </summary>
    [DataContract]
    public class ExSuppVersions
    {
        [DataMember]
        public int nzp_version { get; set; }
        //версия
        [DataMember]
        public string version { get; set; }
        //целочисленное представление версии
        [DataMember]
        public int iversion { get; set; }
        //список изменений
        [DataMember]
        public string list_changes { get; set; }

    }
    /// <summary>
    /// список версий с списком изменений
    /// </summary>
    [DataContract]
    public class ExSuppErrorsType
    {
        [DataMember]
        public int nzp_err { get; set; }
        //описание ошибки
        [DataMember]
        public string error { get; set; }
    }


    /// <summary>
    /// реестр загрузок файлов со сведениями о начислениях от поставщиков услуг
    /// </summary>
    [DataContract]
    public class ReestrExSuppCharge
    {
        [DataMember]
        public int nzp_reestr { get; set; }
        [DataMember]
        public string file_name { get; set; }
        //12ный код поставщика
        [DataMember]
        public string kod_supp { get; set; }
        //месяц за который производится выгрузка
        [DataMember]
        public DateTime saldo_date { get; set; }
        //номер версии
        [DataMember]
        public int version { get; set; }
        [DataMember]
        public int num_file { get; set; }
        //дата выгрузки
        [DataMember]
        public DateTime date_unload { get; set; }
        [DataMember]
        public string user_name { get; set; }
        [DataMember]
        public int nzp_user { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public int status { get; set; }
        [DataMember]
        public string status_name { get; set; }
        [DataMember]
        public string errors { get; set; }
        [DataMember]
        public int nzp_err { get; set; }
        //ссылка на скачивание выгрузки
        [DataMember]
        public string file_link { get; set; }
        //ссылка на файл протокола
        [DataMember]
        public string prot_link { get; set; }
        //ссылка на лог
        [DataMember]
        public int nzp_log { get; set; }
        //кол-во записей с начислениями
        [DataMember]
        public int count_rows { get; set; }
        //кол-во записей загруженных в систему
        [DataMember]
        public int count_rows_inserted { get; set; }
        //  начислено к оплате в файле
        [DataMember]
        public decimal all_sum_charge { get; set; }
        // всего загружено начислений в систему
        [DataMember]
        public decimal all_sum_charge_inserted { get; set; }
        //кол-во показаний ПУ в файле
        [DataMember]
        public int count_pu { get; set; }
        //число загруженных показаний ПУ 
        [DataMember]
        public int count_pu_inserted { get; set; }

    }


    public enum StatusUnload
    {
        /// <summary>
        /// Ошибка при выгрузке
        /// </summary>
        Fail = 0,
        /// <summary>
        /// в процессе 
        /// </summary>
        InProcess = 1,
        /// <summary>
        /// успешно выгружено 
        /// </summary>
        Success = 2
    }

    #endregion

    /// <summary>
    /// Класс для работы с таблицей central_data.simple_load
    /// </summary>
    [DataContract]
    public class SimpleLoadClass
    {
        public enum DownLoadStatus
        {
            /// <summary>
            /// Загружено с ошибками
            /// </summary>
            Errors = 0,
            /// <summary>
            /// Загружено успешно 
            /// </summary>
            Success = 1,
            /// <summary>
            /// В процессе загрузки
            /// </summary>
            InProgress = 2,
            /// <summary>
            /// Не загружен
            /// </summary>
            NotLoaded = 3
        }

        public enum ParsingStatus
        {
            /// <summary>
            /// Пусто
            /// </summary>
            Empty = 0,
            /// <summary>
            /// Сохранен в БД начислений 
            /// </summary>
            Saved = 1
        }

        public string GeNameDownLoadStatus(int status)
        {
            if (status == SimpleLoadClass.DownLoadStatus.Errors.GetHashCode()) return "Загружено с ошибками";
            else if (status == SimpleLoadClass.DownLoadStatus.Success.GetHashCode()) return "Загружено успешно";
            else if (status == SimpleLoadClass.DownLoadStatus.InProgress.GetHashCode()) return "Загружается";
            else if (status == SimpleLoadClass.DownLoadStatus.NotLoaded.GetHashCode()) return "Не загружено";
            else return "";
        }

        public string GeNameParsingStatus(int status)
        {
            if (status == SimpleLoadClass.ParsingStatus.Empty.GetHashCode()) return "Пусто";
            else if (status == SimpleLoadClass.ParsingStatus.Saved.GetHashCode()) return "Сохранен в БД начислений";
            else return "";
        }

        [DataMember]
        public int nzp_load { get; set; }
        [DataMember]
        public int nzp_exc { get; set; }
        [DataMember]
        public string SourceOrg { get; set; }
        [DataMember]
        public string UserSourceOrg { get; set; }
        [DataMember]
        public string file_name { get; set; }
        [DataMember]
        public string temp_file { get; set; }
        [DataMember]
        public decimal percent { get; set; }
        [DataMember]
        public string created_on { get; set; }
        [DataMember]
        public int created_by { get; set; }
        [DataMember]
        public int month_ { get; set; }
        [DataMember]
        public int year_ { get; set; }
        [DataMember]
        public string calc_month { get; set; }
        [DataMember]
        public int nzp_wp { get; set; }
        [DataMember]
        public int nzp { get; set; }
        [DataMember]
        public int tip { get; set; }
        [DataMember]
        public string point { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public int parsing_status { get; set; }
        [DataMember]
        public int download_status { get; set; }
        [DataMember]
        public string parsing_status_name { get { return GeNameParsingStatus(parsing_status); } set { } }
        [DataMember]
        public string download_status_name { get { return GeNameDownLoadStatus(download_status); } set { } }
        [DataMember]
        public string num { get; set; }
        [DataMember]
        public string num_pack { get; set; }
        [DataMember]
        public string sum_pack { get; set; }
        [DataMember]
        public string dat_pack { get; set; }

        [DataMember]
        public string data_type { get; set; }


        public SimpleLoadClass()
            : base()
        {
            nzp_load = 0;
            nzp_exc = 0;
            SourceOrg = "";
            UserSourceOrg = "";
            file_name = "";
            temp_file = "";
            percent = 0;
            created_on = "";
            created_by = 0;
            month_ = 0;
            year_ = 0;
            parsing_status = 0;
            download_status = 0;
            nzp_supp = 0;
            name_supp = "";
            nzp = 0;
            tip = 0;
            data_type = "";
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <summary>
        /// Загрузка оплат от ВТБ24
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns UploadVTB24(FilesImported finder);

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
    }

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
    public class ExFinder : Finder
    {
        [DataMember]
        public bool success { get; set; }

        [DataMember]
        public int VTB24ReestrID { get; set; }
        
        [DataMember]
        public int Status { get; set; }

        [DataMember]
        public int nzp_bank { get; set; }
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
}
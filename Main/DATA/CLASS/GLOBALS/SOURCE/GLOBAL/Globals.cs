using System;
using System.Text;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using System.IO;
using Globals.SOURCE.INTF;

namespace STCLINE.KP50.Global
{
    //----------------------------------------------------------------------
    [DataContract(Namespace = Constants.Linespace, Name = "Retcode")]
    public struct Returns //возвращаемый результат
    //----------------------------------------------------------------------
    {
        [DataMember(Name = "result", Order = 0)]
        public bool result;
        [DataMember(Name = "text", Order = 1)]
        public string text;
        [DataMember(Name = "tag", Order = 2)]
        public int tag;
        [DataMember(Name = "sql_error", Order = 3)]
        public string sql_error;

        public Returns(bool _result, string _text, int _tag)
        {
            result = _result;
            text = _text;
            sql_error = "";
            tag = _tag;
        }

        public Returns(bool _result, string _text)
        {
            result = _result;
            text = _text;
            sql_error = "";
            tag = 0;
        }

        public Returns(bool _result)
        {
            result = _result;
            text = "";
            sql_error = "";
            tag = 0;
        }
    };

  

    //----------------------------------------------------------------------
    static public class CalcThreads
    //----------------------------------------------------------------------
    {
        public static int maxCalcThreads = 11; //11;
        //public static int curCalcThreads = 4; //11;
    }


    //----------------------------------------------------------------------
    static public class Connections
    //----------------------------------------------------------------------
    {
        public static string cert_portal = "";
        public static string cert_client = "";

        public static int max_connections = 200;
        private static int cur_connection = 0;

        /// <summary>
        /// Признак, разрешена ли одновременная работа нескольких пользователей под одним логином
        /// </summary>
        public static bool IsAllowedOneUserHasSeveralSessions = false;

        public static bool Inc_connection()
        {
            if (!Valid_connection())
                return false;

            Interlocked.Increment(ref cur_connection);
            return (Valid_connection());
        }
        public static void Dec_connection()
        {
            Interlocked.Decrement(ref cur_connection);
        }
        public static void Set_connection(int value)
        {
            Interlocked.Exchange(ref cur_connection, value);
        }
        public static bool Valid_connection()
        {
            return (cur_connection <= max_connections);
        }
    }

    /// <summary>
    /// Регионы
    /// </summary>
    public static class Regions
    {
        /// <summary>
        /// Справочник регионов
        /// </summary>
        public enum Region
        {
            None = 0,
            Mari_el = 12,
            Rso = 15,
            Tatarstan = 16,
            Astrakhan = 30,
            Belgorodskaya_obl = 31,
            Kaluga = 40,
            Orel = 57,
            Samarskaya_obl = 63,
            Tulskaya_obl = 71,
            Irkutsk = 38,
 	        Sverdlovskaya_obl = 66,
            StPetersburg = 78
        }

        /// <summary>
        /// Возвращает регион по коду
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Region GetById(int id)
        {
            if (Enum.IsDefined(typeof(Region), id))
            {
                return (Region)id;
            }

            return Region.None;
        }

    }


    /// <summary>
    /// Рольприложения (хост, брокер, мультихост)
    /// </summary>
    public static class SrvRunProgramRole
    {

        public static bool IsHost;
        public static bool IsBroker;
        public static bool IsMulti;
    }

  
   

    //Ксласс объекта информации об боновлениях
    public class UpData //Данные об обновлении
    {
        double version;
        string Status;
        string Date;
        string TypeUp;
        string path;
        string Key;
        string Soup;
        string Nzp;
        string Web_path;
        string Report;

        [DataMember]
        public double Version
        {
            set { version = value; }
            get { return version; }
        }

        [DataMember]
        public string status
        {
            set { Status = value; }
            get { return Status; }
        }

        [DataMember]
        public string date
        {
            set { Date = value; }
            get { return Date; }
        }

        [DataMember]
        public string typeUp
        {
            set { TypeUp = value; }
            get { return TypeUp; }
        }

        [DataMember]
        public string Path
        {
            set { path = value; }
            get { return path; }
        }

        [DataMember]
        public string key
        {
            set { Key = value; }
            get { return Key; }
        }

        [DataMember]
        public string soup
        {
            set { Soup = value; }
            get { return Soup; }
        }

        [DataMember]
        public string nzp
        {
            set { Nzp = value; }
            get { return Nzp; }
        }

        [DataMember]
        public string web_path
        {
            set { Web_path = value; }
            get { return Web_path; }
        }

        [DataMember]
        public string report
        {
            set { Report = value; }
            get { return Report; }
        }

        public UpData()
        {
            this.version = -1;
            this.Status = "NO DATA";
            this.TypeUp = "NO DATA";
            this.path = "NO DATA";
            this.Key = "NO DATA";
            this.Soup = "NO DATA";
            this.Nzp = "NO DATA";
            this.Web_path = "NO DATA";
            this.Report = "NO DATA";

        }
    }



    public static class WCFParams
    {
        //        public static WCFParamsType wcfParams;
        public static WCFParamsType AdresWcfWeb;
        public static WCFParamsType AdresWcfHost;
    }

  


    public class srv_Base
    {
        public srv_Base()
        {
            Utils.setCulture();
        }

        private DateTime queryStartTime;
        private string operationContractName = "";

        protected void BeforeStartQuery(string operationContractName)
        {
            queryStartTime = DateTime.Now;
            this.operationContractName = operationContractName;
        }

        protected void AfterStopQuery()
        {
            TimeSpan ts = DateTime.Now - queryStartTime;
            if (ts > new TimeSpan(0, 0, 5))
            {
                MonitorLog.WriteLog("Время выполнения функции " + operationContractName + " составило " + ts, MonitorLog.typelog.Warn, true);
            }
        }
    }

    /// <summary>
    /// Глобальные настройки системы
    /// </summary>
    public static class GlobalSettings
    {
        /// <summary>
        /// Признак, что работать надо только с центральным банком данных и не обращаться к локальным
        /// </summary>
        public static bool WorkOnlyWithCentralBank = false;

        /// <summary>
        /// режим генерации платежных кодов
        /// </summary>
        public static bool NewGeneratePkodMode = false;
    }





}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Global
{
    [Serializable]
    [DataContract]
    //----------------------------------------------------------------------
    public class Finder //базовый класс для объекта поиска
    //----------------------------------------------------------------------
    {
        string _pref;
        string _point;
        string _trace;
        string _uname;
        string _login;
        string _remoteLogin;
        string _database;
        private string _DateOper;
        [DataMember]
        //дополнительные шаблоны поиска - затем сделать поизящнее
        public List<string> dopFind;
        [DataMember]
        //список выбранных банков данных
        public List<int> dopPointList;
        [DataMember]
        //предыдущая страница
        public int prevPage;
        [DataMember]
        public List<_RolesVal> RolesVal;

        [DataMember]
        public int nzp_user { get; set; }

        /// <summary>
        /// Код пользователя в основном банке данных
        /// </summary>
        [DataMember]
        public int nzp_user_main { get; set; }
        [DataMember]
        public int skip { get; set; }
        [DataMember]
        public int sortby { get; set; }
        [DataMember]
        public int rows { get; set; }
        [DataMember]
        public string pref { get { return Utils.ENull(_pref); } set { _pref = value; } }
        [DataMember]
        public string point { get { return Utils.ENull(_point); } set { _point = value; } }
        [DataMember]
        public int nzp_wp { get; set; }
        [DataMember]
        public int nzp_server { get; set; }
        [DataMember]
        public string trace { get { return Utils.ENull(_trace); } set { _trace = value; } }
        [DataMember]
        public string database { get { return Utils.ENull(_database); } set { _database = value; } }

        [DataMember]
        public string webUname { get { return Utils.ENull(_uname); } set { _uname = value; } }
        [DataMember]
        public string webLogin { get { return Utils.ENull(_login); } set { _login = value; } }
        /// <summary> Логин пользователя, использующийся на удаленном сервере в режиме мультихостинга
        /// </summary>
        [DataMember]
        public string remoteLogin { get { return Utils.ENull(_remoteLogin); } set { _remoteLogin = value; } }
        [DataMember]
        public string date_begin { get; set; }
        [DataMember]
        public int nzp_role { get; set; }
        [DataMember]
        //банк
        public string bank { get; set; }

        [DataMember]
        public List<_OrderingField> orderings { get; set; }

        /// <summary>
        /// Номер выбранного списка (лицевых счетов, домов и т.д.)
        /// </summary>
        [DataMember]
        public int listNumber { get; set; }

        /// <summary>
        /// Проверять блокировку данных: 1 - да, иначе - нет
        /// </summary>
        [DataMember]
        public int checkDataBlocking { get; set; }

        [DataMember]
        public string comment_action { get; set; }


        [DataMember]
        public bool getOperDayByAgent { get; set; }

        [DataMember]
        public string DateOper
        {
            get { return _DateOper; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _DateOper = value;
                    getOperDayByAgent = true;
                }
            }
        }

        public Finder()
        {
            nzp_wp = Constants._ZERO_;
            nzp_server = Constants._ZERO_;

            nzp_user = 0;
            nzp_user_main = 0;
            webUname = "";
            webLogin = "";
            _remoteLogin = "";

            skip = 0;
            sortby = 0;
            rows = 0;
            pref = "";

            dopFind = null;
            dopPointList = null;
            RolesVal = null;

            prevPage = 0;
            date_begin = "";
            database = "";
            nzp_role = 0;
            bank = "";
            orderings = null;

            checkDataBlocking = 1;
            listNumber = -1;
            comment_action = "";
        }

        public void CopyTo(Finder destination)
        {
            if (destination == null) return;

            destination.nzp_wp = nzp_wp;
            destination.nzp_server = nzp_server;

            destination.nzp_user = nzp_user;
            destination.webUname = webUname;
            destination.webLogin = webLogin;
            destination._remoteLogin = _remoteLogin;

            destination.skip = skip;
            destination.sortby = sortby;
            destination.rows = rows;
            destination.pref = pref;

            destination.dopFind = dopFind;
            destination.dopPointList = dopPointList;
            destination.RolesVal = RolesVal;

            destination.prevPage = prevPage;
            destination.date_begin = date_begin;
            destination.database = database;
            destination.nzp_role = nzp_role;

            destination.orderings = orderings;
        }

        public bool InPointList(int nzp_wp)
        {
            return dopPointList == null || dopPointList.Any(p => nzp_wp == p);
        }

    }

    [DataContract]
    public class OperDayForProv
    {
        [DataMember]
        public int nzp_wp { get; set; }
        [DataMember]
        public string pref { get; set; }
        [DataMember]
        public string dats_uchet { get; set; }
        [DataMember]
        public List<DateTime> list_dat_uchet { get; set; }
        [DataMember]
        public int year { get; set; }
        [DataMember]
        public int month { get; set; }

        public OperDayForProv()
        {
            nzp_wp = 0;
            pref = "";
            dats_uchet = "";
            year = 0;
            month = 0;
            list_dat_uchet = new List<DateTime>();
        }
    }
}

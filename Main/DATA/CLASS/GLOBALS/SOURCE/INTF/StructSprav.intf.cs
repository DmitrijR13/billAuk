using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace STCLINE.KP50.Interfaces
{
    /// <summary>
    /// Ссылка между страницами
    /// </summary>
    public class _PageShow
    {
        public int cur_page;
        public int page_url;
        public int up_kod;
        public int sort_kod;
        public string img_url;

        public _PageShow()
        {
            cur_page = 0;
            page_url = 0;
            up_kod = 0;
            sort_kod = 0;
            img_url = "";
        }
    }

    public struct _RolePages //права доступа для nzp_user
    //----------------------------------------------------------------------
    {
        public int id;
        public int nzp_page;
        public int nzp_role;
    }
    public struct _RoleActions//права доступа для nzp_user
    //----------------------------------------------------------------------
    {
        public int id;
        public int nzp_page;
        public long nzp_act;
        public int nzp_role;
    }
    //----------------------------------------------------------------------
    public struct _Actions //действия
    //----------------------------------------------------------------------
    {
        public int nzp_act;
        public string act_name;
        public string act_hlp;
    };
    //----------------------------------------------------------------------
    public struct _ActShow //отображение страница - действие
    //----------------------------------------------------------------------
    {
        public int cur_page;
        public int nzp_act;
        public int act_tip;     //тип действий: 0-меню, 1-чекбокслист, 2-дропдаунлист
        public int act_dd;      //группировка дропдаунлистов(act_tip=2): 1-actmenu, 3,4-в списках кол-во строк или сортировка
        public int sort_kod;
        public string img_url;
    };
    //----------------------------------------------------------------------
    public struct _ActLnk //связка страница - действие
    //----------------------------------------------------------------------
    {
        public int cur_page;
        public int nzp_act;
        public int page_url;
    };
    //----------------------------------------------------------------------
    public struct _SysPort //настройка портала
    //----------------------------------------------------------------------
    {
        public int num_prtd;
        public string val_prtd;
    };
    //----------------------------------------------------------------------
    [DataContract]
    public struct _Arms //АРМы для nzp_user
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_role { get; set; }
        [DataMember]
        public int page_url { get; set; }
        [DataMember]
        public string role { get; set; }
        [DataMember]
        public string img_url { get; set; }
        [DataMember]
        public string url { get; set; }
    };
    //----------------------------------------------------------------------
    public struct _Roles //права доступа для nzp_user
    //----------------------------------------------------------------------
    {
        public int nzp_role;
        public int cur_page;
        public long kod;
        public int tip;
    };
    public struct _Menu //Меню
    //----------------------------------------------------------------------
    {
        public int cur_page;
        public int page_url;
        //public string prm_url;
        public int up_kod;
        public int sort_kod;
    }
    /// <summary>
    /// права доступа для nzp_user
    /// </summary>
    [DataContract]
    public struct _RolesVal
    {
        [DataMember]
        public int nzp_role { get; set; }
        [DataMember]
        public int tip { get; set; }
        [DataMember]
        public long kod { get; set; }
        [DataMember]
        public string val { get; set; }
    };


    //----------------------------------------------------------------------
    public struct _ExtMM //главное меню в ExtJS
    //----------------------------------------------------------------------
    {
        public int nzp_mm;
        public string mm_text;
        public int mm_sort;
    };
    //----------------------------------------------------------------------
    public struct _ExtPM //подменю в ExtJS
    //----------------------------------------------------------------------
    {
        public int nzp_pm;
        public int nzp_mm;
        public string pm_text;
        public string pm_action;
        public string pm_control;
        public int pm_sort;
    };

    //----------------------------------------------------------------------
    public struct _Pages //страница aspx
    //----------------------------------------------------------------------
    {
        public int nzp_page;
        public string page_url;
        public string page_menu;
        public string page_name;
        public string page_help;
        public int? group_id;
    };
    //----------------------------------------------------------------------
    [DataContract]
    public struct _Help //помощь
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_hlp { get; set; }
        [DataMember]
        public int cur_page { get; set; }
        [DataMember]
        public int tip { get; set; }
        [DataMember]
        public int kod { get; set; }
        [DataMember]
        public int sort { get; set; }
        [DataMember]
        public string hlp { get; set; }
    };
    //----------------------------------------------------------------------
    [DataContract]
    public struct _Service
    //----------------------------------------------------------------------
    {
        //service_small, service_name, ed_izmer, type_lgot, nzp_frm, ordering "+
        string _service;
        string _service_small;
        string _service_name;
        string _ed_izmer;

        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public int nzp_cnt { get; set; }
        [DataMember]
        public string service { get { return Utils.ENull(_service); } set { _service = value; } }
        [DataMember]
        public int _checked { get; set; }

        [DataMember]
        public string service_small { get { return Utils.ENull(_service_small); } set { _service_small = value; } }
        [DataMember]
        public string service_name { get { return Utils.ENull(_service_name); } set { _service_name = value; } }
        [DataMember]
        public string ed_izmer { get { return Utils.ENull(_ed_izmer); } set { _ed_izmer = value; } }

        [DataMember]
        public int type_lgot { get; set; }
        [DataMember]
        public int nzp_frm { get; set; }
        [DataMember]
        public int nzp_measure { get; set; }
        [DataMember]
        public int ordering { get; set; }
    }


    [DataContract]
    public struct _Point   //банк данных (s_point)
    //----------------------------------------------------------------------
    {
        string _pref;
        string _point;
        string _bd_old;

        [DataMember]
        public int nzp_graj { get; set; }
        [DataMember]
        public int n { get; set; }
        [DataMember]
        public int flag { get; set; }
        [DataMember]
        public int nzp_wp { get; set; }
        [DataMember]
        public string point { get { return Utils.ENull(_point); } set { _point = value; } }
        [DataMember]
        public string pref { get { return Utils.ENull(_pref); } set { _pref = value; } } //либо префикс ifmx-базы, либо путь к fdb-базе
        [DataMember]
        public string ol_server { get { return Utils.ENull(_bd_old); } set { _bd_old = value; } }
        [DataMember]
        public string b_kod_erc;

        [DataMember]
        public RecordMonth BeginWork; //дата начала работы системы
        [DataMember]
        public RecordMonth BeginCalc; //дата начала расчетов (глубина перерасчетов)

        [DataMember]
        public int nzp_server { get; set; }  //местоположение сервера БД

        [DataMember]
        public RecordMonth CalcMonth;

        //доступные расчетные месяцы
        [DataMember]
        public List<RecordMonth> CalcMonths;

    };

    /// <summary>
    /// Режимы перерасчета начислений
    /// </summary>
    public enum RecalcModes
    {
        /// <summary>
        /// Не задан
        /// </summary>
        None = 0,

        /// <summary>
        /// Автоматический перерасчет начислений при изменении параметров, влияющих на расчет
        /// </summary>
        Automatic = 1,

        /// <summary>
        /// Автоматический перерасчет начислений при изменении параметров, но с возможностью отмены перерасчета
        /// </summary>
        AutomaticWithCancelAbility = 2,

        /// <summary>
        /// Перерасчеты выполняются только по требованию пользователя
        /// </summary>
        Manual = 3
    }


    [DataContract]
    public class CalcMonthParams
    {
        [DataMember]
        public string pref { get; set; }

        public CalcMonthParams()
            : base()
        {
            pref = "";
        }

        public CalcMonthParams(string _pref)
        {
            pref = _pref;
        }
    }


    /// <summary>
    /// Перечислитель 
    /// </summary>
    [DataContract]
    public enum FunctionsTypesGeneratePkod
    {
        /// <summary>
        /// Стандартная генерация
        /// </summary>
        [EnumMember]
        standart = 1,

        /// <summary>
        /// Для Самары
        /// </summary>
        [EnumMember]
        samara = 2,

        /// <summary>
        /// Для Татарстана
        /// </summary>
        [EnumMember]
        tat = 3
    }


    //----------------------------------------------------------------------
    public class LogHis //история сессии
    //----------------------------------------------------------------------
    {
        public LogHis()
        {
            nzp_page = 0;
            kod1 = 0;
            kod2 = 0;
            kod3 = 0;
            idses = "";
        }
        public int nzp_page { get; set; }
        public string idses { get; set; }
        public int kod1 { get; set; }
        public int kod2 { get; set; }
        public int kod3 { get; set; }
    }


    /// <summary>
    /// Зарезервированные подрядчики
    /// </summary>
    public enum Payers
    {
        /// <summary>
        /// Ручной платеж
        /// </summary>
        ManualPayment = 1998,

        /// <summary>
        /// Диспетчерская
        /// </summary>
        DispatchingOffice = 79997,

        /// <summary>
        /// Безналичный платеж
        /// </summary>
        NonCashPayment = 79998,

        /// <summary>
        /// Суперпачка
        /// </summary>
        Superpack = 79999
    }
}

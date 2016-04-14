using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using STCLINE.KP50.Global;
using System.ServiceModel.Activation;
using System.Data;


namespace STCLINE.KP50.Interfaces
{
    /*
    [ServiceContract]
    public interface I_HttpAdres
    {
        [OperationContract]
        //[WebGet(UriTemplate = "json/{id}")]
        [WebInvoke(Method="GET",
            ResponseFormat = WebMessageFormat.Xml,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "xml/{id}")]
        string XMLData(string id);

        [OperationContract]
        [WebInvoke(
            Method="GET",
            ResponseFormat= WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "json/{id}")]
        string JSONData(string id);
    }
    */

    [ServiceContract]
    public interface I_Adres
    {
        [OperationContract]
        int UpdateLs(Ls finder, out Returns ret);

        [OperationContract]
        Returns GenerateLsPu(Ls finder, List<Counter> CounterList);

        [OperationContract]
        int UpdateDom(Dom finder, out Returns ret);
        [OperationContract]
        void UpdateGroupDom(Dom finder, out Returns ret);
        [OperationContract]
        void UpdateGroupLs(Ls finder, out Returns ret);

        [OperationContract]
        List<Ls> GetLs(Ls finder, enSrvOper srv, out Returns ret);

        [OperationContract]
        List<Ls> LoadLs(Ls finder, out Returns ret);

        [OperationContract]
        List<Dom> GetDom(Dom finder, enSrvOper srv, out Returns ret);

        [OperationContract]
        List<Ulica> GetUlica(Dom finder, enSrvOper srv, out Returns ret);

        [OperationContract]
        Returns SaveUlica(Ulica finder);

        [OperationContract]
        List<Ulica> UlicaLoad(Ulica finder, out Returns ret);

        [OperationContract]
        Prefer GetPrefer(out Returns ret);

        [OperationContract]
        GetSelectListDomInfo GetSelectListDomInfo(Finder finder, out Returns ret);     

        [OperationContract]
        List<_Area> GetArea(Finder finder, out Returns ret);

        [OperationContract]
        Returns SaveArea(Area finder);

        [OperationContract]
        List<_Geu> GetGeu(Finder finder, out Returns ret);

        [OperationContract]
        Returns SaveGeu(Geu finder);

        [OperationContract]
        _Rekvizit GetLsRevizit(Ls finder, out Returns ret);


        [OperationContract]
        bool SaveLsRevizit(string pref, _Rekvizit uk, out Returns ret);

        [OperationContract]
        string GetFakturaName(Ls finder, out Returns ret);

        [OperationContract]
        string GetKolGil(MonthLs finder, out Returns ret);


        [OperationContract]
        Dom FindDomFromPm(_Placemark placemark, out Returns ret);

        [OperationContract]
        string GetMapKey(out Returns ret);

        [OperationContract]
        _Placemark GetDefaultPlacemark(out Returns ret);

        [OperationContract]
        List<MapObject> GetMapObjects(MapObject finder, out Returns ret);

        [OperationContract]
        bool SaveMapObjects(List<MapObject> mapObjects, out Returns ret);

        [OperationContract]
        bool DeleteMapObjects(MapObject finder, out Returns ret);



        [OperationContract]
        List<Finder> GetPointsLs(Finder finder, out Returns ret);

        [OperationContract]
        Returns UpdateLsInCache(Ls finder);

        [OperationContract]
        Returns Generator(List<Prm> listprm, int nzp_user);

        [OperationContract]
        Returns Generator2(List<int> listint, int nzp_user, int yy, int mm);

        [OperationContract]
        List<_RajonDom> FindRajonDom(Finder finder, out Returns ret);

        [OperationContract]
        List<Group> GetListGroup(Group finder, out Returns ret);

        [OperationContract]
        List<Group> GetGroupLs(Group finder, enSrvOper srv, out Returns ret);

        [OperationContract]
        List<Group> LoadCurrentLsGroup(Group finder, out Returns ret);

        [OperationContract]
        bool SaveLsGroup(Group finder, List<string> groupList, out Returns ret);

        [OperationContract]
        Returns CreateNewGroup(Group finder);

        [OperationContract]
        bool SaveListGroup(Group finder, out Returns ret);

        [OperationContract]
        List<Search_Info> GetSearchInfo(Ls finder, out Returns ret);

        [OperationContract]
        Returns MakeOperation(Finder finder, Operations oper);

        [OperationContract]
        List<Ls> GetUniquePointAreaGeu(Ls finder, out Returns ret);

        [OperationContract]
        List<Vill> LoadVill(Vill finder, out Returns ret);

        [OperationContract]
        List<Rajon> LoadVillRajon(Rajon finder, out Returns ret);

        [OperationContract]
        Returns SaveVillRajon(Rajon finder, List<Rajon> list_checked);

        [OperationContract]
        Returns GeneratePkod();

        [OperationContract]
        DataTable PrepareLsPuVipiska(Ls finder, out Returns ret);

        [OperationContract]
        Returns UpdateSosLS(Ls finder);

        [OperationContract]
        DataTable PrepareGubCurrCharge(Charge finder, int reportId, out Returns ret);

        [OperationContract]
        Returns UpdateAddressPrefer(Ls finder);

        [OperationContract]
        Ls LoadAddressPrefer(Finder finder, out Returns ret);
    }

    public enum Operations
    {
        /// <summary>
        /// Обновить адресное пространство
        /// </summary>
        RefreshAdresses = 1
    }

    //----------------------------------------------------------------------
    [DataContract]
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

        //дополнительные шаблоны поиска - затем сделать поизящнее
        [DataMember]
        public List<string> dopFind;

        //список выбранных банков данных
        [DataMember]
        public List<int> dopPointList;

        //предыдущая страница
        [DataMember]
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

        //банк
        [DataMember]
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
            if (dopPointList == null)
                return true;

            foreach (int p in dopPointList)
            {
                if (nzp_wp == p)
                    return true;
            }

            return false;
        }
    }
    //----------------------------------------------------------------------

    /// <summary>
    /// Страна
    /// </summary>
    [DataContract]
    public class Land : Finder
    {
        [DataMember]
        public int nzp_land { get; set; }

        [DataMember]
        public string land { get; set; }

        public Land()
            : base()
        {
            nzp_land = 0;
            land = "";
        }
    }

    /// <summary>
    /// Регион
    /// </summary>
    [DataContract]
    public class Stat : Land
    {
        [DataMember]
        public int nzp_stat { get; set; }

        [DataMember]
        public string stat { get; set; }

        public Stat()
            : base()
        {
            nzp_stat = 0;
            stat = "";
        }
    }

    /// <summary>
    /// Город / район
    /// </summary>
    [DataContract]
    public class Town : Finder
    {
        [DataMember]
        public int nzp_town { get; set; }

        [DataMember]
        public string town { get; set; }

        [DataMember]
        public int nzp_stat { get; set; }

        [DataMember]
        public string stat { get; set; }

        [DataMember]
        public int nzp_land { get; set; }

        [DataMember]
        public string land { get; set; }

        [DataMember]
        public int _checked { get; set; }

        [DataMember]
        public string num { get; set; }

        public virtual string getAddress()
        {
            return town;
        }

        public Town()
            : base()
        {
            nzp_town = 0;
            town = land = stat = "";
            nzp_stat = 0;
            nzp_land = 0;
            _checked = 0;
            num = "";
        }

        public void CopyTo(Town destination)
        {
            if (destination == null) return;

            base.CopyTo(destination);

            destination.nzp_town = nzp_town;
            destination.town = town;
            destination.nzp_stat = nzp_stat;
            destination.nzp_land = nzp_land;
            destination._checked = _checked;
            destination.num = num;
        }
    }

    /// <summary>
    /// Муниципальное образование
    /// </summary>
    [DataContract]
    public class Vill : Town
    {
        [DataMember]
        public decimal nzp_vill { get; set; }

        [DataMember]
        public string vill { get; set; }

        public Vill()
            : base()
        {
            nzp_vill = 0;
            vill = "";
        }

        public void CopyTo(Vill destination)
        {
            if (destination == null) return;

            base.CopyTo(destination);

            destination.nzp_vill = nzp_vill;
            destination.vill = vill;
        }
    }

    /// <summary>
    /// Населенный пункт
    /// </summary>
    [DataContract]
    public class Rajon : Vill
    {
        [DataMember]
        public int nzp_raj { get; set; }
        [DataMember]
        public string rajon { get; set; }
        [DataMember]
        public int mark { get; set; }
        [DataMember]
        public int mode { get; set; }

        public override string getAddress()
        {
            string address = base.getAddress();
            address += (address != "" ? ", " : "") + rajon;
            return address;
        }

        public Rajon()
            : base()
        {
            nzp_raj = 0;
            rajon = "";
            mark = 0;
            mode = 0;
        }

        public void CopyTo(Rajon destination)
        {
            if (destination == null) return;

            base.CopyTo(destination);

            destination.nzp_raj = nzp_raj;
            destination.rajon = rajon;
            destination.mark = mark;
            destination.mode = mode;
        }
    }

    /// <summary>
    /// Улица
    /// </summary>
    [DataContract]
    public class Ulica : Rajon
    {
        string _adr;
        string _ulica;
        string _spls;
        string _ulicareg;
        string _area;
        string _geu;

        [DataMember]
        public int nzp_ul { get; set; }
        [DataMember]
        public string ulica { get { return Utils.ENull(_ulica); } set { _ulica = value; } }
        [DataMember]
        public string ulicareg { get { return Utils.ENull(_ulicareg); } set { _ulicareg = value; } }

        [DataMember]
        public int nzp_area { get; set; }
        [DataMember]
        public string area { get { return Utils.ENull(_area); } set { _area = value; } }
        [DataMember]
        public List<int> list_nzp_area { get; set; }
        [DataMember]
        public List<int> list_nzp_wp { get; set; }

        [DataMember]
        public int nzp_geu { get; set; }
        [DataMember]
        public string geu { get { return Utils.ENull(_geu); } set { _geu = value; } }

        [DataMember]
        public string spls { get { return Utils.ENull(_spls); } set { _spls = value; } }
        [DataMember]
        public string adr { get { return Utils.ENull(_adr); } set { _adr = value; } }

        public override string getAddress()
        {
            string address = base.getAddress();
            address += (address != "" ? ", " : "") + ulica_short;
            return address;
        }

        public virtual string getAddressFromUlica()
        {
            return ulica_short;
        }

        public string ulica_short
        {
            get
            {
                return (ulica == null || ulica == "" || ulica == "-" ? "код " + nzp_ul : ulica) + " " + (ulicareg ?? "");
            }
            set { }
        }

        public string ulica_full
        {
            get
            {
                return ulica_short + " / " + (rajon ?? "") + " / " + (town ?? "");
            }
            set { }
        }

        public Ulica()
            : base()
        {
            nzp_ul = Constants._ZERO_;
            nzp_raj = Constants._ZERO_;
            nzp_town = Constants._ZERO_;
            nzp_area = Constants._ZERO_;

            ulica = "";
            ulicareg = "";
            spls = "";
            adr = "";
            _area = "";
            nzp_geu = Constants._ZERO_;
            geu = "";
            list_nzp_area = new List<int>();
            list_nzp_wp = new List<int>();

        }

        public void CopyTo(Ulica destination)
        {
            if (destination == null) return;

            base.CopyTo(destination);

            destination.nzp_ul = nzp_ul;
            destination._ulica = _ulica;
            destination._ulicareg = _ulicareg;
            destination.nzp_area = nzp_area;
            destination.area = area;
            destination._spls = _spls;
            destination._adr = _adr;
        }
    }

    /// <summary>
    /// Дом
    /// </summary>
    [DataContract]
    public class Dom : Ulica
    {
        string _ndom;
        string _nkor;
        string _ndom_po;
        string _remark;

        [DataMember]
        public int nzp_dom { get; set; }

        [DataMember]
        public string ndom { get { return Utils.ENull(_ndom); } set { _ndom = value; } }

        [DataMember]
        public string nkor { get { return Utils.ENull(_nkor); } set { _nkor = value; } }
        [DataMember]
        public string ndom_po { get { return Utils.ENull(_ndom_po); } set { _ndom_po = value; } }

        private _Placemark _placemark;

        [DataMember]
        public float pm_x { get { return _placemark.x; } set { _placemark.x = value; } }
        [DataMember]
        public float pm_y { get { return _placemark.y; } set { _placemark.y = value; } }
        [DataMember]
        public string pm_note { get { return Utils.ENull(_placemark.note); } set { _placemark.note = value; } }

        [DataMember]
        public int chekexistdom { get; set; } // 0 - не проверять существование дома по адресу, 1- проверять

        [DataMember]
        public string prms { get; set; }

        [DataMember]
        public int mark_dom { get; set; }

        [DataMember]
        public int is_blocked { get; set; }// заблокирован ли л/с, 0- нет

        [DataMember]
        public string has_pu { get; set; }

        [DataMember]
        public string remark { get { return Utils.ENull(_remark); } set { _remark = value; } }// примечание

        [DataMember]
        public bool clear_remark { get; set; }

        [DataMember]
        public int num_page { get; set; }

        public void setPlacemark(_Placemark placemark)
        {
            // if (placemark) _placemark = new _Placemark();
            _placemark = placemark;
        }

        public override string getAddress()
        {
            string address = base.getAddress();
            getDomAddress(ref address);
            return address;
        }

        public override string getAddressFromUlica()
        {
            string address = base.getAddressFromUlica();
            getDomAddress(ref address);
            return address;
        }

        private void getDomAddress(ref string address)
        {
            address += ", дом " + ndom;
            if (nkor != "" && nkor != "-") address += ", корп. " + nkor;
        }

        public Dom()
            : base()
        {
            nzp_dom = Constants._ZERO_;

            nzp_land = Constants._ZERO_;
            nzp_stat = Constants._ZERO_;
            chekexistdom = 1;
            num_page = 0;

            ndom = "";
            ndom_po = "";
            nkor = "";
            is_blocked = 0;
            town = "";

            prms = "";
            has_pu = "";
            remark = "";
        }

    }

    /// <summary>
    /// Лицевой счет
    /// </summary>
    [DataContract]
    public class Ls : Dom
    {
        string _pkod;
        string _stypek;
        string _state;
        string _fio;
        string _nkvar;
        string _nkvar_n;
        string _uch;
        string _porch;
        string _phone;
        string _nkvar_po;
        string _remark;

        /// <summary>
        /// Состояния ЛС
        /// </summary>
        public enum States
        {
            /// <summary>
            /// Свойство не задано
            /// </summary>
            None = 0,

            /// <summary>
            /// Открыт
            /// </summary>
            Open = 1,

            /// <summary>
            /// Закрыт
            /// </summary>
            Closed = 2,

            /// <summary>
            /// Состояние не определено
            /// </summary>
            Undefined = 3
        }

        [DataMember]
        public int gil_kol { get; set; }

        [DataMember]
        public int nzp_kvar { get; set; }
       
        [DataMember]
        public int num_ls { get; set; }
        [DataMember]
        public int pkod10 { get; set; }
        [DataMember]
        public string num_ls_litera { get; set; }
        [DataMember]
        public string pkod { get { return Utils.ENull(_pkod); } set { _pkod = value; } }

        [DataMember]
        public string stypek { get { return Utils.ENull(_stypek); } set { _stypek = value; } }

        /// <summary>
        /// состояние ЛС
        /// </summary>
        [DataMember]
        public string state { get { return Utils.ENull(_state); } set { _state = value; } }

        /// <summary>
        /// код состояния ЛС
        /// </summary>
        [DataMember]
        public int stateID { get; set; }

        /// <summary>
        /// перенос ЛС в новую УК/ЖЭУ
        /// </summary>
        [DataMember]
        public bool moving { get; set; }

        /// <summary>
        /// коды состояния ЛС (при множественном выборе)
        /// </summary>
        [DataMember]
        public List<int> stateIDs { get; set; }

        /// <summary>
        /// дата, на которую/с которой действует состояние ЛС
        /// </summary>
        [DataMember]
        public string stateValidOn { get; set; }

        [DataMember]
        public string fio { get { return Utils.ENull(_fio); } set { _fio = value; } }
        [DataMember]
        public int typek { get; set; }

        [DataMember]
        public string nkvar { get { return Utils.ENull(_nkvar); } set { _nkvar = value; } }
        [DataMember]
        public string nkvar_n { get { return Utils.ENull(_nkvar_n); } set { _nkvar_n = value; } }
        [DataMember]
        public string uch { get { return Utils.ENull(_uch); } set { _uch = value; } }
        [DataMember]
        public string porch { get { return Utils.ENull(_porch); } set { _porch = value; } }
        [DataMember]
        public string phone { get { return Utils.ENull(_phone); } set { _phone = value; } }
        [DataMember]
        public string nkvar_po { get { return Utils.ENull(_nkvar_po); } set { _nkvar_po = value; } }
        [DataMember]
        public string remark { get { return Utils.ENull(_remark); } set { _remark = value; } }

        [DataMember]
        public int is_pasportist { get; set; }
        [DataMember]
        public List<Prm> dopParams { get; set; }

        /// <summary>
        /// 0 - не проверять существование лс по адресу, 1- проверять
        /// </summary>
        [DataMember]
        public int chekexistls { get; set; }
        [DataMember]
        public string dat_calc { get; set; }

        /// <summary>
        /// 0 - не генерировать параметры и услуги по номеру ЛС из поля genLsFrim, 1- генерировать
        /// </summary>
        [DataMember]
        public int copy_ls { get; set; }

        /// <summary>
        /// num_ls ЛС, по которому генерируются параметры и услуги
        /// </summary>
        [DataMember]
        public int copy_ls_from { get; set; }

        /// <summary>
        /// 0 - не генерировать ПУ, 1- генерировать
        /// </summary>
        [DataMember]
        public int gen_pu { get; set; }

        /// <summary>
        /// Квартира с (для фильтра)
        /// </summary>
        [DataMember]
        public int kvar_s { set; get; }

        /// <summary>
        /// Квартира по (для фильтра)
        /// </summary>
        [DataMember]
        public int kvar_po { set; get; }

        [DataMember]
        public int ikvar { set; get; }

        /// <summary>
        /// Вернуть группы - используется в LoadKvarList
        /// </summary>
        [DataMember]
        public bool is_get_group { set; get; }

        public override string getAddress()
        {
            string address = base.getAddress();
            getLsAddress(ref address);
            return address;
        }

        public override string getAddressFromUlica()
        {
            string address = base.getAddressFromUlica();
            getLsAddress(ref address);
            return address;
        }

        private void getLsAddress(ref string address)
        {
            if (nkvar != "" && nkvar != "-") address += ", кв. " + nkvar;
            if (nkvar_n != "" && nkvar_n != "-") address += ", комн. " + nkvar_n;
        }

        public Ls()
            : base()
        {
            nzp_kvar = Constants._ZERO_;
            num_ls = Constants._ZERO_;
            num_ls_litera = "";
            pkod = "";
            typek = Constants._ZERO_;
            stateID = Constants._ZERO_;
            moving = false;
            stateValidOn = "";
            fio = "";
            nkvar = "";
            nkvar_n = "";
            uch = "";
            porch = "";
            phone = "";
            nkvar_po = "";

            remark = "";
            chekexistls = 1;
            is_pasportist = 0;
            mark = 1;

            gil_kol = 0;

            dopParams = null;
            dat_calc = "";

            copy_ls = 0;
            copy_ls_from = 0;
            gen_pu = 0;
            kvar_s = 0;
            kvar_po = 0;
        }
    }

    /// <summary>
    /// класс для отчетов
    /// </summary>
    [DataContract]
    public class LsReport : Ls
    {
        /// <summary>
        /// год с
        /// </summary>
        [DataMember]
        public string yearFrom { set; get; }

        /// <summary>
        /// год по
        /// </summary>
        [DataMember]
        public string yearTo { set; get; }

        /// <summary>
        /// месяц с
        /// </summary>
        [DataMember]
        public string monthFrom { set; get; }

        /// <summary>
        /// месяц по
        /// </summary>
        [DataMember]
        public string monthTo { set; get; }
    }

    //----------------------------------------------------------------------
    public struct _KodERC
    //----------------------------------------------------------------------
    {
        public int kod_erc { get; set; }
        public string erc { get; set; }
        public string bdf { get; set; }
    }
    //----------------------------------------------------------------------
    [DataContract]
    public struct _Area
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_area { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public string area { get; set; }
        [DataMember]
        public int _checked { get; set; }
    }

    [DataContract]
    public class Area : Finder
    {
        [DataMember]
        public int nzp_area { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public string area { get; set; }

        public Area()
            : base()
        {
            nzp_area = 0;
            nzp_supp = 0;
            area = "";
        }
    }

    //----------------------------------------------------------------------
    public class Areas //
    //----------------------------------------------------------------------
    {
        public List<_Area> AreaList = new List<_Area>(); //
    }
    //----------------------------------------------------------------------
    [DataContract]
    public struct _Geu
    //----------------------------------------------------------------------
    {
        [DataMember]
        public long nzp_geu { get; set; }
        [DataMember]
        public string geu { get; set; }
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public int _checked { get; set; }
    }
    //----------------------------------------------------------------------
    public class Geus //
    //----------------------------------------------------------------------
    {
        public List<_Geu> GeuList = new List<_Geu>(); //
    }

    [DataContract]
    public class Geu : Finder
    {
        [DataMember]
        public int nzp_geu { get; set; }
        [DataMember]
        public string geu { get; set; }
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public int _checked { get; set; }

        public Geu()
            : base()
        {
            nzp_geu = 0;
            geu = "";
            num = 0;
            _checked = 0;
        }
    }

    //----------------------------------------------------------------------
    [DataContract]
    public struct _Placemark
    //----------------------------------------------------------------------
    {
        [DataMember]
        public long nzp_dom { get; set; }

        [DataMember]
        public float x { get; set; }
        [DataMember]
        public float y { get; set; }
        [DataMember]
        public int nzp_wp { get; set; }
        [DataMember]
        public string note { get; set; }
        [DataMember]
        public int nzp_user { get; set; }
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class _Rekvizit : Ls
    //----------------------------------------------------------------------
    {

        [DataMember]
        public string bank { get; set; }
        [DataMember]
        public string bik { get; set; }
        [DataMember]
        public string korr_schet { get; set; }
        [DataMember]
        public string rschet { get; set; }
        [DataMember]
        public string inn { get; set; }
        [DataMember]
        public string poluch { get; set; }
        [DataMember]
        public string code_uk { get; set; }
        /// <summary>
        /// Второй набор реквизитов для случаев когда деньги
        /// перечисляются ЕРЦ, а потом на счет ТСЖ
        /// </summary>
        [DataMember]
        public string bank2 { get; set; }
        [DataMember]
        public string bik2 { get; set; }
        [DataMember]
        public string korr_schet2 { get; set; }
        [DataMember]
        public string rschet2 { get; set; }
        [DataMember]
        public string inn2 { get; set; }
        [DataMember]
        public string poluch2 { get; set; }
        [DataMember]
        public string code_uk2 { get; set; }
        [DataMember]
        public string dogovor { get; set; }
        [DataMember]
        public string doc_number { get; set; }
        [DataMember]
        public string doc_date { get; set; }
        [DataMember]
        public string dolgnost_ruk { get; set; }
        [DataMember]
        public string fio_ruk { get; set; }
        [DataMember]
        public string fio_ruk_full { get; set; }
        [DataMember]
        public string dolgnost_buh { get; set; }
        [DataMember]
        public string fio_buh { get; set; }
        [DataMember]
        public string phone2 { get; set; }
        [DataMember]
        public string adres { get; set; }
        [DataMember]
        public string adres2 { get; set; }
        [DataMember]
        public int filltext { get; set; }
        [DataMember]
        public string remark { get; set; }

        public _Rekvizit()
            : base()
        {

            bank = "";
            bik = "";
            korr_schet = "";
            rschet = "";
            inn = "";
            poluch = "";
            code_uk = "";
            bank2 = "";
            bik2 = "";
            korr_schet2 = "";
            rschet2 = "";
            inn2 = "";
            poluch2 = "";
            code_uk2 = "";
            doc_number = "";
            doc_date = "";
            dolgnost_ruk = "";
            fio_ruk = "";
            fio_ruk_full = "";
            dolgnost_buh = "";
            fio_buh = "";
            phone = "";
            dogovor = "";
            phone2 = "";
            adres = "";
            adres2 = "";
            filltext = 1;
            remark = "";
        }
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class MonthLs : Ls
    //----------------------------------------------------------------------
    {

        [DataMember]
        public DateTime dat_month { get; set; }

        public MonthLs()
            : base()
        {

            dat_month = DateTime.Today;

        }
    }


    //----------------------------------------------------------------------
    [DataContract]
    public class GetSelectListDomInfo
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int count_ls { get; set; }

        [DataMember]
        public int count_dom { get; set; }

        [DataMember]
        public List<string> list_area { get; set; }
    }

    /// <summary>
    /// Представляет объекты на карте
    /// </summary>
    [DataContract]
    public class MapObject
    {
        public enum Tip
        {
            none = 0,
            mapKey = -1,
            defaultPoint = -2,
            dom = 1,
            ulica = 2,
            area = 3,
            geu = 4
        }

        public static Tip getTip(int tip)
        {
            switch (tip)
            {
                case (int)Tip.mapKey: return Tip.mapKey;
                case (int)Tip.defaultPoint: return Tip.defaultPoint;
                case (int)Tip.dom: return Tip.dom;
                case (int)Tip.ulica: return Tip.ulica;
                case (int)Tip.area: return Tip.area;
                default: return Tip.none;
            }
        }
        /// <summary>
        /// UID объекта
        /// </summary>
        [DataMember]
        public int nzp_mo { get; set; }
        /// <summary>
        /// тип сущности, к которой привязан объект на карте: -1 - ключ для карты, -2 - координаты местности по умолчанию, 1 - дом, 2 - улица, 3 - территория
        /// </summary>
        [DataMember]
        public Tip tip { get; set; }
        /// <summary>
        /// код сущности, определяемый в зависимости от типа сущности: для tip=1 - nzp_dom, tip=2 - nzp_ul и т.д.
        /// для tip = -1, -2 это поле не заполняется
        /// </summary>
        [DataMember]
        public int kod { get; set; }
        /// <summary>
        /// код банка данных, к которому относится сущность
        /// </summary>
        [DataMember]
        public int nzp_wp { get; set; }
        /// <summary>
        /// Тип объекта: 1 - метка, 2 - ломаная, 3 - многоугольник
        /// </summary>
        [DataMember]
        public int object_type { get; set; }
        /// <summary>
        /// описание
        /// </summary>
        [DataMember]
        public string note { get; set; }
        [DataMember]
        public int nzp_user { get; set; }
        [DataMember]
        public List<_MapPoint> points { get; set; }

        [DataMember]
        public List<int> listNzpMo { get; set; }

        public MapObject()
        {
            nzp_mo = 0;
            tip = Tip.none;
            kod = 0;
            nzp_wp = 0;
            object_type = 0;
            note = "";
            nzp_user = 0;
            points = new List<_MapPoint>();
            listNzpMo = new List<int>();
        }
    }

    /// <summary>
    /// Представляет точку на карте, привязанную к объекту (метке, ломаной, многоугольнику и т.д.)
    /// </summary>
    [DataContract]
    public struct _MapPoint
    {
        /// <summary>
        /// UID точки
        /// </summary>
        [DataMember]
        public int nzp_mp { get; set; }
        /// <summary>
        /// UID объекта, к которому привязана точка
        /// </summary>
        [DataMember]
        public int nzp_mo { get; set; }
        /// <summary>
        /// Долгота
        /// </summary>
        [DataMember]
        public float x { get; set; }
        /// <summary>
        /// Широта
        /// </summary>
        [DataMember]
        public float y { get; set; }
        /// <summary>
        /// Порядковый номер точки внутри объекта (имеет значение для ломаных, многоугольников и т.п.)
        /// </summary>
        [DataMember]
        public int ordering { get; set; }
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class Group : Ls
    //----------------------------------------------------------------------
    {
        [DataMember]
        public string ngroup { get; set; }
        [DataMember]
        public string ngroup2 { get; set; }
        [DataMember]
        public int nzp_group { get; set; }

        public Group()
            : base()
        {
            nzp_group = Constants._ZERO_;
            ngroup = "";
            ngroup2 = "";
        }
    }
    //----------------------------------------------------------------------

    /// <summary> Район дома (появился для Самары)
    /// </summary>
    [DataContract]
    public struct _RajonDom
    {
        [DataMember]
        public int nzp_raj_dom;
        [DataMember]
        public string rajon_dom;
        [DataMember]
        public string alt_rajon_dom;

        public _RajonDom(int _nzp_raj_dom, string _rajon_dom)
        {
            nzp_raj_dom = _nzp_raj_dom;
            rajon_dom = _rajon_dom;
            alt_rajon_dom = "";
        }

        public _RajonDom(int _nzp_raj_dom, string _rajon_dom, string _alt_rajon_dom)
        {
            nzp_raj_dom = _nzp_raj_dom;
            rajon_dom = _rajon_dom;
            alt_rajon_dom = _alt_rajon_dom;
        }
    }


    /// <summary> информация для получения информации о поиске
    /// </summary>
    [DataContract]
    public class Search_Info
    {
        [DataMember]
        public string name;
        [DataMember]
        public string value;
    }

    public enum OrderingDirection
    {
        Ascending = 0,
        Descending = 1
    }

    [DataContract]
    public struct _OrderingField
    {
        string _fieldName;
        OrderingDirection _orderingDirection;

        [DataMember]
        public string fieldName { get { return _fieldName; } set { _fieldName = value; } }
        [DataMember]
        public OrderingDirection orderingDirection { get { return _orderingDirection; } set { _orderingDirection = value; } }

        public _OrderingField(string field)
        {
            _fieldName = field;
            _orderingDirection = OrderingDirection.Ascending;
        }

        public _OrderingField(string field, OrderingDirection order)
        {
            _fieldName = field;
            _orderingDirection = order;
        }
    }
    //----------------------------------------------------------------------
    [DataContract]
    public class Prefer
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_land { get; set; }
        [DataMember]
        public int nzp_stat { get; set; }
        [DataMember]
        public int nzp_town { get; set; }
        [DataMember]
        public int nzp_raj { get; set; }
        [DataMember]
        public string land { get; set; }
        [DataMember]
        public string stat { get; set; }
        [DataMember]
        public string town { get; set; }
        [DataMember]
        public string rajon { get; set; }
    }

 


}


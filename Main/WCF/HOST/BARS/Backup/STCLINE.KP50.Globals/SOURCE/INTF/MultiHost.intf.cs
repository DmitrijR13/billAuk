using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using STCLINE.KP50.Global;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Multi
    {
        [OperationContract]
        List<UserStateOfServers> ServersStateByUser(int nzp_user, enSrvOper oper, out Returns ret);
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class UserStateOfServers   //состояние сервера для данного пользователя
    //----------------------------------------------------------------------
    {
        string _comment;
        string _server;
        string _otklik;

        [DataMember]
        public int cur_state { get; set; }
        //public stateServer cur_state { get; set; }

        [DataMember]
        public int nzp_server { get; set; }
        [DataMember]
        public int cnts { get; set; }

        [DataMember]
        public string server { get { return Utils.ENull(_server); } set { _server = value; } }
        [DataMember]
        public string comment { get { return Utils.ENull(_comment); } set { _comment = value; } }
        [DataMember]
        public string otklik { get { return Utils.ENull(_otklik); } set { _otklik = value; } }

        /*
        [DataMember]
        public string state 
        { 
            get 
            {
                string _task = "";
                switch (cur_state)
                {
                    case 0: _task = ""; break;
                }
                return Utils.ENull(_task); 
            } 
        } 
        */

        public UserStateOfServers(int _nzp_srv, string _srv)
        {
            nzp_server = _nzp_srv;
            _server = _srv;
            _comment = "";
            _otklik = "";

            cur_state = 0; // stateServer.none;
            cnts = 0;

        }
    }

    //----------------------------------------------------------------------
    public enum stateServer
    //----------------------------------------------------------------------
    {
        none = 0,
        working = 1,
        error = 2,
        finish = 3
    }

    //----------------------------------------------------------------------
    [DataContract]
    public struct _RCentr   //РЦ (s_rcentr)
    //----------------------------------------------------------------------
    {
        string _rcentr;
        string _adres;
        string _email;
        string _ruk;

        [DataMember]
        public bool is_valid { get; set; }
        [DataMember]
        public int nzp_rc { get; set; }
        [DataMember]
        public int nzp_raj { get; set; }
        [DataMember]
        public int pref { get; set; }

        [DataMember]
        public string rcentr { get { return Utils.ENull(_rcentr); } set { _rcentr = value; } }
        [DataMember]
        public string adres { get { return Utils.ENull(_adres); } set { _adres = value; } }
        [DataMember]
        public string email { get { return Utils.ENull(_email); } set { _email = value; } }
        [DataMember]
        public string ruk { get { return Utils.ENull(_ruk); } set { _ruk = value; } }
    };

    //----------------------------------------------------------------------
    [DataContract]
    public struct _RServer   //сервер БД РЦ (servers)
    //----------------------------------------------------------------------
    {
        string _ip_adr;
        string _login;
        string _pwd;
        string _rcentr;

        [DataMember]
        public bool is_valid { get; set; }
        [DataMember]
        public int nzp_rc { get; set; }
        [DataMember]
        public string rcentr { get { return Utils.ENull(_rcentr); } set { _rcentr = value; } }

        [DataMember]
        public int nzp_server { get; set; }
        [DataMember]
        public string ip_adr { get { return Utils.ENull(_ip_adr); } set { _ip_adr = value; } }
        [DataMember]
        public string login { get { return Utils.ENull(_login); } set { _login = value; } } //
        [DataMember]
        public string pwd { get { return Utils.ENull(_pwd); } set { _pwd = value; } } //

    };




    //----------------------------------------------------------------------
    public class ServersByUser   //
    //----------------------------------------------------------------------
    {
        public int nzp_user { get; set; }

        public List<UserStateOfServers> ListStateOfServers; //список состояний серверов для данного пользователя

        public stateServer cur_state { get; set; }

        //++++++++++++++++++++++++++++++++++++++++++++++++++
        //найти номер позции nzp_server в ListStateByUser
        //++++++++++++++++++++++++++++++++++++++++++++++++++
        int GetSateOfServerID(int nzp_server)
        {
            int i = -1;

            foreach (UserStateOfServers zap in ListStateOfServers)
            {
                i += 1;
                if (zap.nzp_server == nzp_server)
                    return i;
            }

            return i;
        }
        //++++++++++++++++++++++++++++++++++++++++++++++++++
        //записать новое состояние
        //++++++++++++++++++++++++++++++++++++++++++++++++++
        public void PutSateOfServer(UserStateOfServers state_bu)
        {
            int id = GetSateOfServerID(state_bu.nzp_server);

            //изменить состояние
            ListStateOfServers[id] = state_bu; 
        }

    };

    //----------------------------------------------------------------------
    public static class UserServerInfo
    //----------------------------------------------------------------------
    {
        static object syncRoot = new object();
        static int count_users = 0; //кол-во пользовтелей
        public static int count_server = 0; //кол-во серверов
        public static List<ServersByUser> ListUserServera = new List<ServersByUser>();

        //++++++++++++++++++++++++++++++++++++++++++++++++++
        //найти в списке пользователя, если нет - добавить
        //++++++++++++++++++++++++++++++++++++++++++++++++++
        public static int GetServerID(int nzp_user)
        {
            //заблокируем пока доступ к списку при чтении данных
            lock (syncRoot)
            {
                int i = -1;
                foreach (ServersByUser zap in ListUserServera)
                {
                    if (nzp_user == zap.nzp_user)
                    {
                        i += 1;
                        return i;
                    }
                }

                //добавить в список
                count_users += 1;

                //инициализировать состояние серверов 
                ServersByUser server_bu = new ServersByUser();
                server_bu.nzp_user = nzp_user;

                List<UserStateOfServers> listUserState = new List<UserStateOfServers>();

                count_server = 0;
                //список серверов - загнать состояние по-умолчанию
                foreach (_RServer zap in MultiHost.RServers)
                {
                    count_server += 1;
                    UserStateOfServers state_bu = new UserStateOfServers(zap.nzp_server, zap.rcentr);
                    listUserState.Add(state_bu);
                }
                server_bu.ListStateOfServers = listUserState;

                ListUserServera.Add(server_bu);
                //ListServerByUser[sbu_len] = sbu;
                return count_users-1;
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++
        //получить данные по серверам
        //++++++++++++++++++++++++++++++++++++++++++++++++++
        public static ServersByUser GetServerByUser(int nzp_user)
        {
            int sbu_id = GetServerID(nzp_user);
            if (sbu_id >-1)
                return ListUserServera[sbu_id]; 

            //not found
            return new ServersByUser();
        }

    }

    //----------------------------------------------------------------------
    public static class MultiHost
    //----------------------------------------------------------------------
    {
        public static List<_RServer> RServers = new List<_RServer>(); //список remote-северов РЦ
        public static List<_RCentr>  RCentr   = new List<_RCentr>();  //список расчетных центров

        public static bool IsMultiHost = false;


        //определение коннекта при мультихостинге
        public static _RServer GetServer(int nzp_server)
        {
            foreach (_RServer zap in RServers)
            {
                if (nzp_server == zap.nzp_server)
                {
                    return zap;
                }
            }
            return new _RServer(); //ошибка!
        }

    }

}

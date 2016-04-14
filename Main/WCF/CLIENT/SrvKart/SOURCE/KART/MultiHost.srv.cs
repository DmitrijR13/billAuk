using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;

using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;

namespace STCLINE.KP50.Server
{
    public class srv_Multi : srv_Base, I_Multi
    {
        public List<UserStateOfServers> ServersStateByUser(int nzp_user, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<UserStateOfServers> res = null;

            ret = Utils.InitReturns();
            try
            {
                switch (oper)
                {
                    case enSrvOper.SrvFind: //проверить доступ
                        res = CheckServerByUser(nzp_user).ListStateOfServers;
                        break;
                    case enSrvOper.SrvGet: //текущее состояние
                        res = UserServerInfo.GetServerByUser(nzp_user).ListStateOfServers;
                        break;
                    default:
                        res = null;
                        break;
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += ": " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка ServersStateByUser(" + oper.ToString() + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }

            return res;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++
        //запустить параллельные проверки доступа ко всем серверам
        //++++++++++++++++++++++++++++++++++++++++++++++++++
        public static ServersByUser CheckServerByUser(int nzp_user)
        {
            //найти запись пользователя
            ServersByUser server_bu = UserServerInfo.GetServerByUser(nzp_user);

            for (int i = 0; i < UserServerInfo.count_server; i++)
            {
                int list_state_id = i;

                UserStateOfServers state_of_server = server_bu.ListStateOfServers[list_state_id];
                if (state_of_server.cur_state.ToString() != stateServer.working.ToString())
                {
                    //запустить проверку сервера
                    System.Threading.Thread thServer =
                                    new System.Threading.Thread(delegate() { CallCheckServer(nzp_user, list_state_id); });
                    thServer.Start();
                }
            }

            //подождать 2 секунды и вернуть текущее состяние
            System.Threading.Thread.Sleep(2000);
            //server_bu = GetServerByUser(nzp_user);
            return UserServerInfo.GetServerByUser(nzp_user);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++
        static void CallCheckServer(int nzp_user, int list_state_id)
        //++++++++++++++++++++++++++++++++++++++++++++++++++
        {
            //найти запись пользователя
            ServersByUser server_bu = new ServersByUser();
            UserStateOfServers state_of_server = new UserStateOfServers(0, "");

            server_bu = UserServerInfo.GetServerByUser(nzp_user);
            state_of_server = server_bu.ListStateOfServers[list_state_id];

            state_of_server.cur_state = Convert.ToInt32(stateServer.working);
            DateTime d = new DateTime();
            d = DateTime.Now;

            state_of_server.comment = "выполняется проверка доступа....";
            state_of_server.otklik = "-";
            server_bu.ListStateOfServers[list_state_id] = state_of_server;

            //вызов remote-сервиса
            int nzp_server = state_of_server.nzp_server;

            Returns ret = Utils.InitReturns();
            cli_Sprav cli = new cli_Sprav(nzp_server);
            List<_ResY> ResYList = new List<_ResY>();
            ResYList = cli.ResYLoad(out ret);
            if (ret.result)
            {
                state_of_server.cur_state = Convert.ToInt32(stateServer.finish);
                state_of_server.comment = "сервер доступен";
            }
            else
            {
                state_of_server.cur_state = Convert.ToInt32(stateServer.error);
                state_of_server.comment = "сервер не доступен";
            }

            state_of_server.otklik = (DateTime.Now - d).Seconds.ToString() + " сек.";
            server_bu.ListStateOfServers[list_state_id] = state_of_server;
        }
        
    }
}

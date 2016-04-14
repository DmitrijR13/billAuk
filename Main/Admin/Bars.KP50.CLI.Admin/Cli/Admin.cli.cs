using System;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.ServiceModel;
using System.Data;
using System.IO;
using System.Reflection;

namespace STCLINE.KP50.Client
{
    public class cli_Admin : cli_Base, I_Admin
    {
        public cli_Admin(int nzp_server)
            : base(nzp_server)
        {
            //_cli_Admin(nzp_server);
        }

        //void _cli_Admin(int nzp_server)
        //{
        //    _RServer zap = MultiHost.GetServer(nzp_server);
        //    string addrHost = "";

        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        //определить параметры доступа
        //        addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvAdmin;
        //        remoteObject = HostChannel.CreateInstance<I_Admin>(zap.login, zap.pwd, addrHost);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvAdmin;
        //        zap.rcentr = "<локально>";
        //        remoteObject = HostChannel.CreateInstance<I_Admin>(addrHost);
        //    }

        //    //Попытка открыть канал связи
        //    try
        //    {
        //        ICommunicationObject proxy = remoteObject as ICommunicationObject;
        //        proxy.Open();
        //    }
        //    catch (Exception ex)
        //    {
        //        MonitorLog.WriteLog(string.Format("Ошибка открытия Proxy {0} по адресу {1}. Сервер {2} (Код РЦ {3}) (Код сервера {4}): {5}",
        //                            System.Reflection.MethodBase.GetCurrentMethod().Name,
        //                            addrHost,
        //                            zap.rcentr,
        //                            zap.nzp_rc,
        //                            nzp_server,
        //                            ex.Message),
        //                            MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //}

        //~cli_Admin()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}

        IAdminRemoteObject getRemoteObject()
        {
            return getRemoteObject<IAdminRemoteObject>(WCFParams.AdresWcfWeb.srvAdmin);
        }

        public Returns RemoveUserLock(Finder WebUserId)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.RemoveUserLock(WebUserId);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка RemoveUserLock\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка RemoveUserLock\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public static bool DbIsUserHasRole(Role finder, out Returns ret)
        {
            bool b;
            using (DbAdminClient db = new DbAdminClient())
            {
                b = db.IsUserHasRole(finder, out ret);
            }
            return b;
        }

        public static Returns DbIsPasswordRecoveryRequestValid(User finder)
        {
            Returns ret;
            using (DbAdminClient db = new DbAdminClient())
            {
                ret = db.isPasswordRecoveryRequestValid(finder);
            }
            return ret;
        }

        public static bool DbAuthenticate(BaseUser web_user, string ip, string browser, string idses, out int inbase, bool UseWSO2Autentification = false)
        {
            bool res = true;
            using (ClassAuthenticateUser db = new ClassAuthenticateUser())
            {
                res = db.AuthenticateUser(web_user, ip, browser, idses, out inbase, UseWSO2Autentification);
            }
            return res;
        }

        public static User DbAddPasswordRecoveryRequest(User finder, out Returns ret)
        {
            User user = null;
            using (DbAdminClient db = new DbAdminClient())
            {
                user = db.AddPasswordRecoveryRequest(finder, out ret);
            }
            return user;
        }

        public static void DBClearTempUserTables(int nzp_user, out Returns ret)
        {
            using (DbAdminClient db = new DbAdminClient())
            {
                db.DBClearTempUserTables(nzp_user, out ret);
            }
        }

        public static SMTPSetup DbGetSMTPSetup(out Returns ret)
        {
            SMTPSetup setup = null;
            using (DbAdminClient db = new DbAdminClient())
            {
                setup = db.GetSMTPSetup(out ret);
            }
            return setup;
        }

        public static User DbSetNewPassword(User finder, out Returns ret)
        {
            User user = null;
            using (DbAdminClient db = new DbAdminClient())
            {
                user = db.SetNewPassword(finder, out ret);
            }
            return user;
        }

        public static bool DbIsResetUserPwd(User finder, out Returns ret)
        {
            bool b = false;
            using (DbAdminClient db = new DbAdminClient())
            {
                b = db.IsResetUserPwd(finder, out ret);
            }
            return b;
        }

        public static void FillingKeyRoles(int nzp_user)
        {
            using (ClassFillUserRigths db = new ClassFillUserRigths())
            {
                db.FillingKeyRoles(nzp_user);
            }
        }

        public static void ReloadUserRights(int nzp_user)
        {
            using (ClassFillUserRigths db = new ClassFillUserRigths())
            {
                db.ReloadUserRights(nzp_user);
            }
        }

        public static Returns DbResetUserPwd(User finder)
        {
            Returns ret;
            using (DbAdminClient db = new DbAdminClient())
            {
                ret = db.ResetUserPwd(finder);
            }
            return ret;
        }

        public static Returns DbAddUserRoles(User user)
        {
            Returns ret;
            using (DbAdminClient db = new DbAdminClient())
            {
                ret = db.AddUserRoles(user);
            }
            return ret;
        }

        /*public static Returns DbRefreshRoles(int nzp_user)
        {
            DbUserClient db = new DbUserClient();
            db.FillingKeyRoles(nzp_user);
            Returns ret = Utils.InitReturns();
            db.Close();
            return ret;
        }*/

        public static Returns DbDeleteUserRoles(User user)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.DeleteUserRoles(user);
            db.Close();
            return ret;
        }

        public static Returns DbSaveUser(User user)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.SaveUser(user);
            db.Close();
            return ret;
        }

        public static bool DbCloseSeans(int nzp_user, string idses)
        {
            bool b = true;
            using (DbUserClient db = new DbUserClient())
            {
                b = db.CloseSeans(nzp_user, idses);
            }
            return b;
        }

        public static bool DbLogAccLast(int nzp_user, out BaseUser bu)
        {
            DbLogClient db = new DbLogClient();
            bool b = db.LogAccLast(nzp_user, out bu);
            db.Close();
            return b;
        }

        //public static void DbGetArms(int nzp_user, ref List<_Arms> Arms)
        //{
        //    DbUserClient db = new DbUserClient();
        //    db.GetArms(nzp_user, ref Arms);
        //    db.Close();
        //}

        //role_pages and role_actions
        public static void DbGetArms(int nzp_user, ref List<_Arms> Arms)
        {
            DbUserClient db = new DbUserClient();
            db.GetArmsNew(nzp_user, ref Arms);
            db.Close();
        }

        public static List<Role> DbGetRoles(Role finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            List<Role> list = db.GetRoles(finder, out ret);
            db.Close();
            return list;
        }

        public static void DbSaveUserRoles(User finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            db.SaveUserRoles(finder, out ret);
            db.Close();
            return;
        }

        public static RolesTree DbGetRolesToTree(RolesTree finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            RolesTree list = db.GetRolesToTree(finder, out ret);
            db.Close();
            return list;
        }

        public static LogsTree DbGetWebLogsList(LogsTree finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            LogsTree list = db.GetWebLogsList(finder, out ret);
            db.Close();
            return list;
        }

        public static LogsTree DbGetWebLogsFile(LogsTree finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            LogsTree logs = db.GetWebLogsFile(finder, out ret);
            db.Close();
            return logs;
        }

        public static List<Role> DbGetSubRoles(Role finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            List<Role> list = db.GetSubRoles(finder, out ret);
            db.Close();
            return list;
        }

        public static List<Role> DbGetRoles(Role finder, out Returns ret, bool isLoadRolesVal)
        {
            DbAdminClient db = new DbAdminClient();
            List<Role> list = db.GetRoles(finder, out ret, isLoadRolesVal);
            db.Close();
            return list;
        }

        //public static void DbGetRoles(int nzp_role, int nzp_user, int cur_page, ref int mod, ref List<_Roles> Roles, string idses)
        //{
        //    DbUserClient db = new DbUserClient();
        //    db.GetRoles(nzp_role, nzp_user, cur_page, ref mod, ref Roles, idses);
        //    db.Close();
        //}

        public static void DbGetRoles(int nzp_role, int nzp_user, int cur_page, ref int mod, ref List<_RoleActions> RoleActions, ref List<_RolePages> RolePages, string idses)
        {
            DbUserClient db = new DbUserClient();
            db.GetRoles(nzp_role, nzp_user, cur_page, ref  mod, ref  RoleActions, ref RolePages, idses);
            db.Close();
        }

        public static void DbGetRolesVal(int nzp_role, int nzp_user, ref List<_RolesVal> RolesVal)
        {
            DbUserClient db = new DbUserClient();
            db.GetRolesVal(nzp_role, nzp_user, ref RolesVal);
            db.Close();
        }

        public static void DbGetRolesKey(int nzp_role, string tip, string kod, int nzp_user, ref List<_RolesVal> RolesVal)
        {
            DbUserClient db = new DbUserClient();
            db.GetRolesKey(nzp_role, tip, kod, nzp_user, ref RolesVal);
            db.Close();
        }

        public static void DbGetRolesKey(int nzp_role, int nzp_user, ref List<_RolesVal> RolesVal)
        {
            DbUserClient db = new DbUserClient();
            db.GetRolesKey(nzp_role, nzp_user, ref RolesVal);
            db.Close();
        }

        public static List<UserAccess> DbGetUserAccess(UserAccess finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            List<UserAccess> list = db.GetUserAccess(finder, out ret);
            db.Close();
            return list;
        }
        public static List<UserActions> DbGetUsersActionsList(UserActions finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            List<UserActions> list = db.GetUsersActionsList(finder, out ret);
            db.Close();
            return list;
        }
        public static List<UserActions> DbGetUsersInActList(Finder finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            List<UserActions> list = db.GetUsersInActList(finder, out ret);
            db.Close();
            return list;
        }
        public static List<UserActions> DbGetPagesInActList(Finder finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            List<UserActions> list = db.GetPagesInActList(finder, out ret);
            db.Close();
            return list;
        }

        public static Returns DbDeleteRolesVal(Role role)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.DeleteRolesVal(role);
            db.Close();
            return ret;
        }

        public static Returns DbSaveRole(Role role)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.SaveRole(role);
            db.Close();
            return ret;
        }

        public static Returns DbSaveStatusRoles(List<Role> spis)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.SaveStatusRoles(spis);
            db.Close();
            return ret;
        }

        public static Returns DbAddRolesVal(Role role)
        {
            Returns ret;
            using (DbAdminClient db = new DbAdminClient())
            {
                ret = db.AddRolesVal(role);
            }
            return ret;
        }

        public static Returns DbLogOutUser(User finder)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.LogOutUser(finder);
            db.Close();
            return ret;
        }

        public static Returns DbDeleteProcessCalc(CalcFonTask proc)
        {
            var db = new DbCalcQueueClient();
            Returns ret = db.DeleteProcessCalc(proc);
            db.Close();
            return ret;
        }

        public static Returns DbChangeUserBlock(User user)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.ChangeUserBlock(user);
            db.Close();
            return ret;
        }

        public static Returns DbDeleteProcessSaldo(SaldoFonTask proc)
        {
            var db = new DbSaldoQueueClient();
            Returns ret = db.DeleteProcessSaldo(proc);
            db.Close();
            return ret;
        }

        public static List<SaldoFonTask> DbGetProcessSaldo(SaldoFonTask finder, out Returns ret)
        {
            var db = new DbSaldoQueueClient();
            List<SaldoFonTask> list = db.GetProcessSaldo(finder, out ret);
            db.Close();
            return list;
        }

        public static List<CalcFonTask> DbGetProcessCalc(CalcFonTask finder, out Returns ret)
        {
            var db = new DbCalcQueueClient();
            List<CalcFonTask> list = db.GetProcessCalc(finder, out ret);
            db.Close();
            return list;
        }


        public static List<BillFonTask> DbGetProcessBill(BillFonTask finder, out Returns ret)
        {
            var db = new DbBillQueueClient();
            ret = new Returns();
            List<BillFonTask> list = db.GetProcessBill(finder, out ret);
            db.Close();
            return list;
        }

        public static Returns DbSaveProcessSaldo(SaldoFonTask proc)
        {
            var db = new DbSaldoQueueClient();
            Returns ret = db.SaveProcessSaldo(proc);
            db.Close();
            return ret;
        }

        public static Returns DbSaveProcessBill(List<BillFonTask> tasks)
        {
            var db = new DbBillQueueClient();
            Returns ret = db.SaveProcessBill(tasks);
            db.Close();
            return ret;
        }

        public static Returns DbDeleteProcessBill(BillFonTask proc)
        {
            var db = new DbBillQueueClient();
            Returns ret = db.DeleteProcessBill(proc);
            db.Close();
            return ret;
        }

        public static BaseUser DbGetUser(User finder)
        {
            DbUserClient db = new DbUserClient();
            BaseUser user;
            try
            {
                user = db.GetUser(finder);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в DbGetUser\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                user = null;
            }
            db.Close();
            return user;
        }

        public ReturnsObjectType<BaseUser> GetUser(User finder)
        {
            ReturnsObjectType<BaseUser> ret;
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUser(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new ReturnsObjectType<BaseUser>(false, "");
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUser\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new ReturnsObjectType<BaseUser>(false, ex.Message);
                MonitorLog.WriteLog("Ошибка GetUser\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public static ReturnsObjectType<List<User>> DbGetUsers(User finder)
        {
            DbAdminClient db = new DbAdminClient();
            ReturnsObjectType<List<User>> ret;
            try
            {
                ret = db.GetUsers(finder);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в DbGetUsers\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                ret = new ReturnsObjectType<List<User>>(false, ex.Message);
            }
            db.Close();

            return ret;
        }

        public ReturnsObjectType<List<User>> GetUsers(User finder)
        {
            ReturnsObjectType<List<User>> ret;
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUsers(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new ReturnsObjectType<List<User>>(false, "");
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUsers\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new ReturnsObjectType<List<User>>(false, "");
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetUsers\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public static Returns DbGetRemoteUser(Finder finder)
        {
            User finderUser = new User();

            finderUser.login = finder.remoteLogin;
            finderUser.is_remote = 1; //добавил явную выборку только тех пользователей, кому разрешен удаленный доступ

            DbUserClient db = new DbUserClient();
            BaseUser user = db.GetUser(finderUser);

            Returns ret;

            if (user != null)
            {
                List<_RolesVal> rolesVal = new List<_RolesVal>();
                db.GetRolesVal(finder.nzp_role, user.nzp_user, ref rolesVal);

                finder.nzp_user = user.nzp_user;
                finder.RolesVal = rolesVal;
                finder.webLogin = user.login;
                finder.webUname = user.uname;

                ret = new Returns(true);
            }
            else
            {
                ret = new Returns(false, "Удаленный пользователь не найден", -1);
            }
            db.Close();
            return ret;
        }

        public static List<_RServer> DbServersAvailableForRole(Role finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            List<_RServer> res = db.ServersAvailableForRole(finder, out ret);
            db.Close();
            return res;
        }

        public static Returns DbDropCacheTables(User finder)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.DropCacheTables(finder);
            db.Close();
            return ret;
        }

        public static List<Setup> DbGetListSetup(Setup finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            List<Setup> res = db.GetListSetup(finder, out ret);
            db.Close();
            return res;
        }

        public static Returns DbSaveSetup(Setup finder)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.SaveSetup(finder);
            db.Close();
            return ret;
        }

        public static List<int> CheckPagePermission(Finder finder, string pages, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            List<int> list = db.CheckPagePermission(finder, pages, out ret);
            db.Close();
            return list;
        }


        // Старый метод загрузки ЖКУ
        //public Returns LoadHarGilFondGKU(FilesImported finder)
        //{
        //    Returns ret = Utils.InitReturns();
        //    try
        //    {
        //        ret = remoteObject.LoadHarGilFondGKU(finder);
        //        HostChannel.CloseProxy(remoteObject);
        //    }
        //    catch (Exception ex)
        //    {
        //        ret.result = false;
        //        ret.text = ex.Message;

        //        string err = "";
        //        if (Constants.Viewerror) err = "\n " + ex.Message;

        //        MonitorLog.WriteLog("Ошибка LoadHarGilFondGKU" + err, MonitorLog.typelog.Error, 2, 100, true);
        //    }

        //    return ret;
        //}


        public Returns SetToChange(ServFormulFinder finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SetToChange(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SetToChange\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SetToChange" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<ServFormulFinder>> GetServFormul(Finder finder)
        {
            ReturnsObjectType<List<ServFormulFinder>> ret = new ReturnsObjectType<List<ServFormulFinder>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetServFormul(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetServFormul\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;
                MonitorLog.WriteLog("Ошибка GetServFormul\n" + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileGilec(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFileGilec(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFileGilec\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFileGilec" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileIpu(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFileIpu(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFileIpu\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFileIpu" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileIpuP(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFileIpuP(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFileIpuP\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFileIpuP" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileOdpu(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFileOdpu(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFileOdpu\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFileOdpu\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileOdpuP(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFileOdpuP(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFileOdpuP\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFileOdpuP" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileNedopost(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFileNedopost(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFileNedopost\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFileNedopost" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileOplats(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFileOplats(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFileOplats\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFileOplats" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileParamDom(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFileParamDom(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFileParamDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFileParamDom" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileParamLs(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFileParamLs(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFileParamLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFileParamLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileTypeNedop(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFileTypeNedop(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFileTypeNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFileTypeNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileTypeParams(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFileTypeParams(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFileTypeParams\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFileTypeParams\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }



        public static Returns DbSaveUserSessionId(BaseUser finder)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.SaveUserSessionId(finder);
            db.Close();
            return ret;
        }

        public List<AreaCodes> GetAreaCodes(AreaCodes finder, out Returns ret)
        {
            List<AreaCodes> res = new List<AreaCodes>();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetAreaCodes(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public Returns GetMaxCodeFromAreaCodes()
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetMaxCodeFromAreaCodes();
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveAreaCodes(AreaCodes finder)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveAreaCodes(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns DeleteAreaCodes(AreaCodes finder)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteAreaCodes(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns CreateSequence()
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.CreateSequence();
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns UploadEFS(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UploadEFS(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public List<EFSReestr> GetEFSReestr(EFSReestr finder, out Returns ret)
        {
            List<EFSReestr> res = new List<EFSReestr>();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetEFSReestr(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<EFSPay> GetEFSPay(EFSPay finder, out Returns ret)
        {
            List<EFSPay> res = new List<EFSPay>();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetEFSPay(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<EFSCnt> GetEFSCnt(EFSCnt finder, out Returns ret)
        {
            List<EFSCnt> res = new List<EFSCnt>();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetEFSCnt(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public Returns DeleteFromEFSReestr(EFSReestr finder)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteFromEFSReestr(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }



        /// <summary>
        /// ПОлучения списка задач
        /// </summary>
        /// <param name="finder">содежит данные для получения списка задач, такие как nzp_user, start, limit</param>
        /// <param name="ret">результат</param>
        /// <returns></returns>
        public static List<Job> DBGetJobs(Finder finder, out Returns ret)
        {
            var db = new DbAdminClient();
            var res = db.GetJobs(finder, out ret);
            db.Close();
            return res;
        }

        public static bool DbRegisterUserAction(int nzpUser, int nzpPage, string pageName, int nzpAct, string actName)
        {
            var db = new DbAdminClient();
            var res = db.RegisterUserAction(nzpUser, nzpPage, pageName, nzpAct, actName);
            db.Close();
            return res;
        }

        /// <summary>
        /// Проверка завершенности задачи
        /// </summary>
        /// <param name="jobId">идентификатор задачи</param>
        /// <returns></returns>
        public static Returns CheckJobEnd(int jobId)
        {
            var db = new DbAdminClient();
            var ret = db.CheckJobEnd(jobId);
            db.Close();
            return ret;
        }
        public List<SysEvents> GetSysEvents(SysEvents finder, out Returns ret)
        {
            List<SysEvents> res = new List<SysEvents>();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetSysEvents(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public List<SysEvents> GetSysEventsUsersList(SysEvents finder, out Returns ret)
        {
            List<SysEvents> res = new List<SysEvents>();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetSysEventsUsersList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<SysEvents> GetSysEventsEventsList(SysEvents finder, out Returns ret)
        {
            List<SysEvents> res = new List<SysEvents>();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetSysEventsEventsList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<SysEvents> GetSysEventsActionsList(SysEvents finder, out Returns ret)
        {
            List<SysEvents> res = new List<SysEvents>();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetSysEventsActionsList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<SysEvents> GetSysEventsEntityList(SysEvents finder, out Returns ret)
        {
            List<SysEvents> res = new List<SysEvents>();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetSysEventsEntityList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<CountersArx> GetCountersChangeHistory(CountersArx finder, out Returns ret)
        {
            List<CountersArx> res = new List<CountersArx>();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetCountersChangeHistory(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<CountersArx> GetCountersFields(CountersArx finder, out Returns ret)
        {
            List<CountersArx> res = new List<CountersArx>();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetCountersFields(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<CountersArx> GetCountersArxUsersList(CountersArx finder, out Returns ret)
        {
            List<CountersArx> res = new List<CountersArx>();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetCountersArxUsersList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public bool InsertSysEvent(SysEvents finder)
        {
            bool res = false;
            var ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.InsertSysEvent(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public LogsTree GetHostLogsList(LogsTree finder, out Returns ret)
        {
            LogsTree res = new LogsTree();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetHostLogsList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public LogsTree GetHostLogsFile(LogsTree finder, out Returns ret)
        {
            LogsTree res = new LogsTree();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetHostLogsFile(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }


        public List<KeyValue> LoadRoleSprav(Role finder, int role_kod, out Returns ret)
        {
            List<KeyValue> list = new List<KeyValue>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.LoadRoleSprav(finder, role_kod, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return list;
        }


        public List<TransferHome> GetHouseList(TransferHome finder, out Returns ret)
        {
            List<TransferHome> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetHouseList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return list;
        }

        public Returns PrepareProvsForFirstCalcPeni(Finder finder)
        {
            var ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.PrepareProvsForFirstCalcPeni(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }


        public DateTime GetDateStartPeni(Finder finder, out Returns ret)
        {
            DateTime startPeniDateTime = new DateTime();
            try
            {
                using (var ro = getRemoteObject())
                {
                    startPeniDateTime = ro.GetDateStartPeni(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return startPeniDateTime;
        }

        public int GetCountDayToDateObligation(Finder finder, out Returns ret)
        {
            int countDay = -1;
            try
            {
                using (var ro = getRemoteObject())
                {
                    countDay = ro.GetCountDayToDateObligation(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return countDay;
        }


        public Returns DeleteCurrentRole(Finder finder)
        {
            var ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteCurrentRole(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }


        public Returns RePrepareProvsOnListLs(Ls finder, TypePrepareProvs type)
        {
            var ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.RePrepareProvsOnListLs(finder, type);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

       
    }
}
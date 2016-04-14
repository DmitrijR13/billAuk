using System;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.ServiceModel;
using System.Data;
using System.IO;

namespace STCLINE.KP50.Client
{
    public class cli_Admin : I_Admin
    {
        I_Admin remoteObject;

        public cli_Admin(int nzp_server)
            : base()
        {
            _cli_Admin(nzp_server);
        }

        void _cli_Admin(int nzp_server)
        {
            _RServer zap = MultiHost.GetServer(nzp_server);
            string addrHost = "";

            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvAdmin;
                remoteObject = HostChannel.CreateInstance<I_Admin>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvAdmin;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Admin>(addrHost);
            }

            //Попытка открыть канал связи
            try
            {
                ICommunicationObject proxy = remoteObject as ICommunicationObject;
                proxy.Open();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(string.Format("Ошибка открытия Proxy {0} по адресу {1}. Сервер {2} (Код РЦ {3}) (Код сервера {4}): {5}",
                                    System.Reflection.MethodBase.GetCurrentMethod().Name,
                                    addrHost,
                                    zap.rcentr,
                                    zap.nzp_rc,
                                    nzp_server,
                                    ex.Message),
                                    MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        ~cli_Admin()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        public ReturnsObjectType<List<UploadingData>> GetUploadingProgress(UploadingData finder)
        {
            ReturnsObjectType<List<UploadingData>> ret;
            try
            {
                ret = remoteObject.GetUploadingProgress(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new ReturnsObjectType<List<UploadingData>>(false, "");

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUploadingProgress\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public ReturnsObjectType<List<FilesImported>> GetFiles(FilesImported finder)
        {
            ReturnsObjectType<List<FilesImported>> ret;
            try
            {
                ret = remoteObject.GetFiles(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new ReturnsObjectType<List<FilesImported>>(false, "");

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetFiles\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns OperateWithFileImported(FilesDisassemble finder, FilesImportedOperations operation)
        {
            Returns ret;
            try
            {
                ret = remoteObject.OperateWithFileImported(finder, operation);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, "");

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка OperateWithFileImported(" + operation + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public static bool DbIsUserHasRole(Role finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            bool b = db.IsUserHasRole(finder, out ret);
            db.Close();
            return b;
        }

        public static Returns DbIsPasswordRecoveryRequestValid(User finder)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.isPasswordRecoveryRequestValid(finder);
            db.Close();
            return ret;
        }

        public static bool DbAuthenticate(BaseUser web_user, string ip, string browser, string idses, out int inbase)
        {
            DbUserClient db = new DbUserClient();
            bool b = db.Authenticate(web_user, ip, browser, idses, out inbase);
            db.Close();
            return b;
        }

        public static User DbAddPasswordRecoveryRequest(User finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            User user = db.AddPasswordRecoveryRequest(finder, out ret);
            db.Close();
            return user;
        }

        public static SMTPSetup DbGetSMTPSetup(out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            SMTPSetup setup = db.GetSMTPSetup(out ret);
            db.Close();
            return setup;
        }

        public static User DbSetNewPassword(User finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            User user = db.SetNewPassword(finder, out ret);
            db.Close();
            return user;
        }

        public static bool DbIsResetUserPwd(User finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            bool b = db.IsResetUserPwd(finder, out ret);
            db.Close();
            return b;
        }

        public static void FillingKeyRoles(int nzp_user)
        {
            DbUserClient db = new DbUserClient();
            db.FillingKeyRoles(nzp_user);
            db.Close();
        }

        public static Returns DbResetUserPwd(User finder)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.ResetUserPwd(finder);
            db.Close();
            return ret;
        }

        public static Returns DbAddUserRoles(User user)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.AddUserRoles(user);
            db.Close();
            return ret;
        }

        public static Returns DbRefreshRoles(int nzp_user)
        {
            DbUserClient db = new DbUserClient();
            db.FillingKeyRoles(nzp_user);
            Returns ret = Utils.InitReturns();
            db.Close();
            return ret;
        }

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
            DbUserClient db = new DbUserClient();
            bool b = db.CloseSeans(nzp_user, idses);
            db.Close();
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
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.AddRolesVal(role);
            db.Close();
            return ret;
        }

        public static Returns DbLogOutUser(User finder)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.LogOutUser(finder);
            db.Close();
            return ret;
        }

        public static Returns DbDeleteProcessCalc(ProcessCalc proc)
        {
            DbAdminClient db = new DbAdminClient();
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

        public static Returns DbDeleteProcessSaldo(ProcessSaldo proc)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.DeleteProcessSaldo(proc);
            db.Close();
            return ret;
        }

        public static List<ProcessSaldo> DbGetProcessSaldo(ProcessSaldo finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            List<ProcessSaldo> list = db.GetProcessSaldo(finder, out ret);
            db.Close();
            return list;
        }

        public static List<ProcessCalc> DbGetProcessCalc(ProcessCalc finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            List<ProcessCalc> list = db.GetProcessCalc(finder, out ret);
            db.Close();
            return list;
        }


        public static List<ProcessBill> DbGetProcessBill(ProcessBill finder, out Returns ret)
        {
            DbAdminClient db = new DbAdminClient();
            ret = new Returns();
            List<ProcessBill> list = db.GetProcessBill(finder, out ret);
            db.Close();
            return list;
        }

        public static Returns DbSaveProcessSaldo(ProcessSaldo proc)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.SaveProcessSaldo(proc);
            db.Close();
            return ret;
        }

        public static Returns DbSaveProcessBill(List<ProcessBill> tasks)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.SaveProcessBill(tasks);
            db.Close();
            return ret;
        }

        public static Returns DbDeleteProcessBill(ProcessBill proc)
        {
            DbAdminClient db = new DbAdminClient();
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
                ret = remoteObject.GetUser(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new ReturnsObjectType<BaseUser>(false, "");

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUser\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
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
                ret = remoteObject.GetUsers(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new ReturnsObjectType<List<User>>(false, "");

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUsers\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
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

        public Returns UploadInDb(FilesImported finder, UploadOperations operation, UploadMode mode)
        {
            Returns ret;
            try
            {
                ret = remoteObject.UploadInDb(finder, operation, mode);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, "");

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UploadInDb(" + operation + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns LoadHarGilFondGKU(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.LoadHarGilFondGKU(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadHarGilFondGKU" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns LoadOneTime(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.LoadOneTime(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadOneTime" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<KLADRData>> LoadDataFromKLADR(KLADRFinder finder)
        {
            ReturnsObjectType<List<KLADRData>> ret = new ReturnsObjectType<List<KLADRData>>();
            try
            {
                ret = remoteObject.LoadDataFromKLADR(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadDataFromKLADR" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns RefreshKLADRFile(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.RefreshKLADRFile(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UploadKLADR" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns UploadUESCharge(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.UploadUESCharge(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UploadUESCharge" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns UploadMURCPayment(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.UploadMURCPayment(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UploadMURCPayment" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<ComparedAreas>> GetComparedArea(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedAreas>>();
            try
            {
                ret = remoteObject.GetComparedArea(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedStreets" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<ComparedSupps>> GetComparedSupp(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedSupps>>();
            try
            {
                ret = remoteObject.GetComparedSupp(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedSupp" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<ComparedVills>> GetComparedMO(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedVills>>();
            try
            {
                ret = remoteObject.GetComparedMO(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedMO" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<ComparedServs>> GetComparedServ(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedServs>>();
            try
            {
                ret = remoteObject.GetComparedServ(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedServ" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }
        public ReturnsObjectType<List<ComparedMeasures>> GetComparedMeasure(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedMeasures>>();
            try
            {
                ret = remoteObject.GetComparedMeasure(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedMeasure" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParType(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();
            try
            {
                ret = remoteObject.GetComparedParType(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedParType" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParBlag(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();
            try
            {
                ret = remoteObject.GetComparedParBlag(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedParBlag" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns ReadReestrFromCbb(FilesImported finderpack, FilesImported finder, string connectionString)
        {
            var ret = new Returns();
            try
            {
                ret = remoteObject.ReadReestrFromCbb( finderpack,  finder,  connectionString);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка ReadReestrFromCbb" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParGas(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();
            try
            {
                ret = remoteObject.GetComparedParGas(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedParGas" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParWater(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();
            try
            {
                ret = remoteObject.GetComparedParWater(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedParWater" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }       

        public ReturnsObjectType<List<ComparedTowns>> GetComparedTown(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedTowns>>();
            try
            {
                ret = remoteObject.GetComparedTown(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedStreets" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<ComparedRajons>> GetComparedRajon(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedRajons>>();
            try
            {
                ret = remoteObject.GetComparedRajon(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedStreets" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<ComparedStreets>> GetComparedStreets(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedStreets>>();
            try
            {
                ret = remoteObject.GetComparedStreets(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedStreets" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<ComparedHouses>> GetComparedHouse(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedHouses>>();
            try
            {
                ret = remoteObject.GetComparedHouse(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedHouse" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<ComparedLS>> GetComparedLS(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedLS>>();
            try
            {
                ret = remoteObject.GetComparedLS(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedLS" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkArea(ComparedAreas finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkArea(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkStreet" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkSupp(ComparedSupps finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkSupp(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkSupp" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkMO(ComparedVills finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkMO(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkMO" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkServ(ComparedServs finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkServ(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkServ" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkParType(ComparedParTypes finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkParType(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkParType" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkParBlag(ComparedParTypes finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkParBlag(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkParBlag" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkParGas(ComparedParTypes finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkParGas(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkParGas" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkParWater(ComparedParTypes finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkParWater(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkParWater" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkMeasure(ComparedMeasures finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkMeasure(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkMeasure" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkTown(ComparedTowns finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkTown(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkStreet" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkRajon(ComparedRajons finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkRajon(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkRajon" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkStreet(ComparedStreets finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkStreet(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkStreet" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkHouse(ComparedHouses finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkHouse(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkHouse" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType UnlinkLS(ComparedLS finder)
        {
            var ret = new ReturnsType();
            try
            {
                ret = remoteObject.UnlinkLS(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkLS" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns SetToChange(ServFormulFinder finder)
        {
            Returns ret = new Returns();
            try
            {
                ret = remoteObject.SetToChange(finder);
                HostChannel.CloseProxy(remoteObject);
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
                ret = remoteObject.GetServFormul(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetServFormul" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedAreas>> GetUncomparedArea(Finder finder)
        {
            ReturnsObjectType<List<UncomparedAreas>> ret = new ReturnsObjectType<List<UncomparedAreas>>();
            try
            {
                ret = remoteObject.GetUncomparedArea(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedArea" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedSupps>> GetUncomparedSupp(Finder finder)
        {
            ReturnsObjectType<List<UncomparedSupps>> ret = new ReturnsObjectType<List<UncomparedSupps>>();
            try
            {
                ret = remoteObject.GetUncomparedSupp(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedSupp" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedVills>> GetUncomparedMO(Finder finder)
        {
            ReturnsObjectType<List<UncomparedVills>> ret = new ReturnsObjectType<List<UncomparedVills>>();
            try
            {
                ret = remoteObject.GetUncomparedMO(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedMO" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedServs>> GetUncomparedServ(Finder finder)
        {
            ReturnsObjectType<List<UncomparedServs>> ret = new ReturnsObjectType<List<UncomparedServs>>();
            try
            {
                ret = remoteObject.GetUncomparedServ(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedServ" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParType(Finder finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                ret = remoteObject.GetUncomparedParType(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedParType" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParBlag(Finder finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                ret = remoteObject.GetUncomparedParBlag(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedParBlag" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParGas(Finder finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                ret = remoteObject.GetUncomparedParGas(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedParGas" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParWater(Finder finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                ret = remoteObject.GetUncomparedParWater(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedParWater" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedMeasures>> GetUncomparedMeasure(Finder finder)
        {
            ReturnsObjectType<List<UncomparedMeasures>> ret = new ReturnsObjectType<List<UncomparedMeasures>>();
            try
            {
                ret = remoteObject.GetUncomparedMeasure(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedMeasure" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedTowns>> GetUncomparedTown(Finder finder)
        {
            ReturnsObjectType<List<UncomparedTowns>> ret = new ReturnsObjectType<List<UncomparedTowns>>();
            try
            {
                ret = remoteObject.GetUncomparedTown(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedTown" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedRajons>> GetUncomparedRajon(Finder finder)
        {
            ReturnsObjectType<List<UncomparedRajons>> ret = new ReturnsObjectType<List<UncomparedRajons>>();
            try
            {
                ret = remoteObject.GetUncomparedRajon(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedRajon" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedStreets>> GetUncomparedStreets(Finder finder)
        {
            ReturnsObjectType<List<UncomparedStreets>> ret = new ReturnsObjectType<List<UncomparedStreets>>();
            try
            {
                ret = remoteObject.GetUncomparedStreets(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedStreets" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedHouses>> GetUncomparedHouse(Finder finder)
        {
            ReturnsObjectType<List<UncomparedHouses>> ret = new ReturnsObjectType<List<UncomparedHouses>>();
            try
            {
                ret = remoteObject.GetUncomparedHouse(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedHouse" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedLS>> GetUncomparedLS(Finder finder)
        {
            ReturnsObjectType<List<UncomparedLS>> ret = new ReturnsObjectType<List<UncomparedLS>>();
            try
            {
                ret = remoteObject.GetUncomparedLS(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedLS" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedAreas>> GetAreaByFilter(UncomparedAreas finder)
        {
            ReturnsObjectType<List<UncomparedAreas>> ret = new ReturnsObjectType<List<UncomparedAreas>>();
            try
            {
                ret = remoteObject.GetAreaByFilter(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetAreaByFilter" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedSupps>> GetSuppByFilter(UncomparedSupps finder)
        {
            ReturnsObjectType<List<UncomparedSupps>> ret = new ReturnsObjectType<List<UncomparedSupps>>();
            try
            {
                ret = remoteObject.GetSuppByFilter(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetSuppByFilter" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedVills>> GetMOByFilter(UncomparedVills finder)
        {
            ReturnsObjectType<List<UncomparedVills>> ret = new ReturnsObjectType<List<UncomparedVills>>();
            try
            {
                ret = remoteObject.GetMOByFilter(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetMOByFilter" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedServs>> GetServByFilter(UncomparedServs finder)
        {
            ReturnsObjectType<List<UncomparedServs>> ret = new ReturnsObjectType<List<UncomparedServs>>();
            try
            {
                ret = remoteObject.GetServByFilter(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetServByFilter" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParTypeByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                ret = remoteObject.GetParTypeByFilter(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetParTypeByFilter" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParBlagByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                ret = remoteObject.GetParBlagByFilter(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetParBlagByFilter" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParGasByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                ret = remoteObject.GetParGasByFilter(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetParGasByFilter" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParWaterByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                ret = remoteObject.GetParWaterByFilter(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetParWaterByFilter" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedMeasures>> GetMeasureByFilter(UncomparedMeasures finder)
        {
            ReturnsObjectType<List<UncomparedMeasures>> ret = new ReturnsObjectType<List<UncomparedMeasures>>();
            try
            {
                ret = remoteObject.GetMeasureByFilter(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetMeasureByFilter" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedTowns>> GetTownByFilter(UncomparedTowns finder)
        {
            ReturnsObjectType<List<UncomparedTowns>> ret = new ReturnsObjectType<List<UncomparedTowns>>();
            try
            {
                ret = remoteObject.GetTownByFilter(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetTownByFilter" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedRajons>> GetRajonByFilter(UncomparedRajons finder)
        {
            ReturnsObjectType<List<UncomparedRajons>> ret = new ReturnsObjectType<List<UncomparedRajons>>();
            try
            {
                ret = remoteObject.GetRajonByFilter(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetRajonByFilter" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedStreets>> GetStreetsByFilter(UncomparedStreets finder)
        {
            ReturnsObjectType<List<UncomparedStreets>> ret = new ReturnsObjectType<List<UncomparedStreets>>();
            try
            {
                ret = remoteObject.GetStreetsByFilter(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetStreetsByFilter" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<List<UncomparedHouses>> GetHouseByFilter(UncomparedHouses finder)
        {
            ReturnsObjectType<List<UncomparedHouses>> ret = new ReturnsObjectType<List<UncomparedHouses>>();
            try
            {
                ret = remoteObject.GetHouseByFilter(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetHouseByFilter" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }


        
        public ReturnsType ChangeTownForRajon(UncomparedRajons finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.ChangeTownForRajon(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка ChangeTownForRajon" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }
        public ReturnsType LinkArea(UncomparedAreas finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.LinkArea(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkArea" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType LinkSupp(UncomparedSupps finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.LinkSupp(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkSupp" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType LinkMO(UncomparedVills finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.LinkMO(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkMO" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType LinkServ(UncomparedServs finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.LinkServ(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkServ" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType LinkParType(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.LinkParType(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkParType" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType LinkParBlag(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.LinkParBlag(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkParBlag" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType LinkParGas(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.LinkParGas(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkParGas" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType LinkParWater(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.LinkParWater(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkParWater" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType LinkMeasure(UncomparedMeasures finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.LinkMeasure(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkMeasure" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType LinkTown(UncomparedTowns finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.LinkTown(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkTown" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType LinkRajon(UncomparedRajons finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.LinkRajon(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkRajon" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType LinkNzpStreet(UncomparedStreets finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.LinkNzpStreet(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkNzpStreet" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType LinkNzpDom(UncomparedHouses finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.LinkNzpDom(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkNzpDom" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType AddNewArea(UncomparedAreas finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.AddNewArea(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewArea" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType AddNewSupp(UncomparedSupps finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.AddNewSupp(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewSupp" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType AddNewMO(UncomparedVills finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.AddNewMO(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewMO" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType AddNewServ(UncomparedServs finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.AddNewServ(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewServ" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType AddNewParType(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.AddNewParType(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewParType" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType AddNewParBlag(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.AddNewParBlag(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewParBlag" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType AddNewParGas(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.AddNewParGas(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewParGas" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }
        public ReturnsType AddNewParWater(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.AddNewParWater(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewParWater" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }


        public ReturnsType AddNewMeasure(UncomparedMeasures finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.AddNewMeasure(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewMeasure" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType AddNewRajon(UncomparedRajons finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.AddNewRajon(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewStreet" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType AddNewStreet(UncomparedStreets finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.AddNewStreet(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewStreet" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType AddNewHouse(UncomparedHouses finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.AddNewHouse(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewHouse" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileGilec(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                ret = remoteObject.GetFileGilec(finder);
                HostChannel.CloseProxy(remoteObject);
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
                ret = remoteObject.GetFileIpu(finder);
                HostChannel.CloseProxy(remoteObject);
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

        public ReturnsObjectType<DownloadedData> GetFileIpuP(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                ret = remoteObject.GetFileIpuP(finder);
                HostChannel.CloseProxy(remoteObject);
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

        public ReturnsObjectType<DownloadedData> GetFileOdpu(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                ret = remoteObject.GetFileOdpu(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFileOdpu" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileOdpuP(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                ret = remoteObject.GetFileOdpuP(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFileOdpu" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileNedopost(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                ret = remoteObject.GetFileNedopost(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFileNedopost" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileOplats(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                ret = remoteObject.GetFileOplats(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFileOplats" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileParamDom(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                ret = remoteObject.GetFileParamDom(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFileParamDom" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileParamLs(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                ret = remoteObject.GetFileParamLs(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFileParamLs" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileTypeNedop(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                ret = remoteObject.GetFileTypeNedop(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFileTypeNedop" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileTypeParams(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();
            try
            {
                ret = remoteObject.GetFileTypeParams(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetFileTypeParams" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }   

        public ReturnsType DbSaveFileToDisassembly(FilesImported finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.DbSaveFileToDisassembly(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка DbSaveFileToDisassembly" + err, MonitorLog.typelog.Error, 2, 100, true);
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


        public Returns UploadGilec(List<int> lst)
        {
            Returns ret;
            try
            {
                ret = remoteObject.UploadGilec(lst);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = Utils.InitReturns();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка функции: UploadGilec\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns AddAllHouse(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                ret = remoteObject.AddAllHouse(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка AddAllHouse" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns LinkStreetAutom(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                ret = remoteObject.LinkStreetAutom(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkStreetAutom" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns LinkRajonAutom(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                ret = remoteObject.LinkRajonAutom(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LinkRajonAutom" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns DeleteUnrelatedInfo()
        {
            Returns ret = new Returns();
            try
            {
                ret = remoteObject.DeleteUnrelatedInfo();
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteUnrelatedInfo" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns UsePreviousLinks(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                ret = remoteObject.UsePreviousLinks(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UsePreviousLinks" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns DbUploadExchangeSZ(Finder finder, DataTable dt)
        {
            DbAdminClient db = new DbAdminClient();
            Returns ret = db.UploadExchangeSZ(finder, dt);
            db.Close();
            return ret;
        }

      

        public Returns DeleteFromExchangeSZ(Finder finder, int nzp_ex_sz)
        {
            var ret = new Returns();
            try
            {
                ret = remoteObject.DeleteFromExchangeSZ(finder, nzp_ex_sz);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteFromExchangeSZ" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public List<AreaCodes> GetAreaCodes(AreaCodes finder, out Returns ret)
        {
            List<AreaCodes> res = new List<AreaCodes>();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetAreaCodes(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetAreaCodes" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public Returns GetMaxCodeFromAreaCodes()
        {
            var ret = new Returns();
            try
            {
                ret = remoteObject.GetMaxCodeFromAreaCodes();
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetMaxCodeFromAreaCodes" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns SaveAreaCodes(AreaCodes finder)
        {
            var ret = new Returns();
            try
            {
                ret = remoteObject.SaveAreaCodes(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveAreaCodes" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns DeleteAreaCodes(AreaCodes finder)
        {
            var ret = new Returns();
            try
            {
                ret = remoteObject.DeleteAreaCodes(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteAreaCodes" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns CreateSequence()
        {
            var ret = new Returns();
            try
            {
                ret = remoteObject.CreateSequence();
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CreateSequence" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns UploadEFS(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                ret = remoteObject.UploadEFS(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UploadEFS" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public List<EFSReestr> GetEFSReestr(EFSReestr finder, out Returns ret)
        {
            List<EFSReestr> res = new List<EFSReestr>();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetEFSReestr(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetEFSReestr" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<EFSPay> GetEFSPay(EFSPay finder, out Returns ret)
        {
            List<EFSPay> res = new List<EFSPay>();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetEFSPay(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetEFSPay" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<EFSCnt> GetEFSCnt(EFSCnt finder, out Returns ret)
        {
            List<EFSCnt> res = new List<EFSCnt>();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetEFSCnt(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetEFSCnt" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public Returns DeleteFromEFSReestr(EFSReestr finder)
        {
            var ret = new Returns();
            try
            {
                ret = remoteObject.DeleteFromEFSReestr(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteFromEFSReestr" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        /// <summary>
        /// Подготовить данные для печати ЛС
        /// </summary>
        /// <param name="finder">объект поиска</param>
        /// <returns>результат</returns>
        public Returns PreparePrintInvoices(List<PointForPrepare> finder)
        {
            var ret = new Returns();
            try
            {
                ret = remoteObject.PreparePrintInvoices(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка PreparePrintInvoices" + err, MonitorLog.typelog.Error, 2, 100, true);
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
                res = remoteObject.GetSysEvents(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetEFSCnt" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<SysEvents> GetSysEventsUsersList(SysEvents finder, out Returns ret)
        {
            List<SysEvents> res = new List<SysEvents>();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetSysEventsUsersList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetSysEventsUsersList" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<SysEvents> GetSysEventsEventsList(SysEvents finder, out Returns ret)
        {
            List<SysEvents> res = new List<SysEvents>();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetSysEventsEventsList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetSysEventsEventList" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<SysEvents> GetSysEventsEntityList(SysEvents finder, out Returns ret)
        {
            List<SysEvents> res = new List<SysEvents>();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetSysEventsEntityList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetSysEventsEntityList" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<CountersArx> GetCountersChangeHistory(CountersArx finder, out Returns ret)
        {
            List<CountersArx> res = new List<CountersArx>();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetCountersChangeHistory(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetCountersChangeHistory" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<CountersArx> GetCountersFields(CountersArx finder, out Returns ret)
        {
            List<CountersArx> res = new List<CountersArx>();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetCountersFields(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetCountersFields" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public List<CountersArx> GetCountersArxUsersList(CountersArx finder, out Returns ret)
        {
            List<CountersArx> res = new List<CountersArx>();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetCountersArxUsersList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetCountersArxUsersList" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public bool InsertSysEvent(SysEvents finder)
        {
            bool res = false;
            var ret = Utils.InitReturns();
            try
            {
                res = remoteObject.InsertSysEvent(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка InsertSysEvent" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public LogsTree GetHostLogsList(LogsTree finder, out Returns ret)
        {
            LogsTree res = new LogsTree();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetHostLogsList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetHostLogsList" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }

        public LogsTree GetHostLogsFile(LogsTree finder, out Returns ret)
        {
            LogsTree res = new LogsTree();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetHostLogsFile(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetHostLogsFile" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return res;
        }
    }
}
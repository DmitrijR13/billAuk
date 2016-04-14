using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
//using System.ServiceModel.Web;
using System.Text;
using System.ServiceModel.Activation;

using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.Diagnostics;
using System.Collections;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
//using SevenZip;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace STCLINE.KP50.Client
{
    public class cli_Patch : I_Patch
    {
        I_Patch remoteObject;

        public cli_Patch()
            : base()
        {
            _cli_Patch(0);
        }

        public cli_Patch(int nzp_server)
            : base()
        {
            _cli_Patch(nzp_server);
        }

        public cli_Patch(string conn, string login, string pass)
        {
            Constants.Login = login;
            Constants.Password = pass;
            remoteObject = HostChannel.CreateInstance<I_Patch>(login, pass, conn + WCFParams.AdresWcfWeb.srvPatch, "Patch");
        }

        void _cli_Patch(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvPatch;
                remoteObject = HostChannel.CreateInstance<I_Patch>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvPatch;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Patch>(Constants.Login, Constants.Password, addrHost, "Patch");
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

        public Dictionary<string, object> GoPatch(ArrayList sql_array, string dataBaseType, byte[] soup)
        {
            try
            {
                Dictionary<string, object> retDic = remoteObject.GoPatch(sql_array, dataBaseType, soup);
                HostChannel.CloseProxy(remoteObject);
                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения патча : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
        }


        public Stream GetMonitorLog(DateTime BeginDate, DateTime EndDate)
        {
            try
            {
                Stream Result = remoteObject.GetMonitorLog(BeginDate, EndDate);
                HostChannel.CloseProxy(remoteObject);
                return Result;
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка получения событий Komplat50log на хосте", MonitorLog.typelog.Error, true);
                return null;
            }
        }

        public void ExecSQLList(Stream List)
        {
            try
            {
                remoteObject.ExecSQLList(List);
                HostChannel.CloseProxy(remoteObject);
                return;
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка выполнения команд псевдоязыка", MonitorLog.typelog.Error, true);
                return;
            }
        }

        public Stream GetSelect()
        {
            try
            {
                Stream Result = remoteObject.GetSelect();
                HostChannel.CloseProxy(remoteObject);
                return Result;
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка выполнения команд псевдоязыка", MonitorLog.typelog.Error, true);
                return null;
            }
        }

        public void FullUpdateStr(string UpdateFile, string UpdateMD5, int UpdateIndex, string WebPath, string pass, string passMD5)
        {
            try
            {
                remoteObject.FullUpdateStr(UpdateFile, UpdateMD5, UpdateIndex, WebPath, pass, passMD5);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка обновления, индекс " + UpdateIndex.ToString() + "; " + ex.Message, MonitorLog.typelog.Error, true);
            }
            return;
        }

        public void FullUpdate(Stream UpdateFile)
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                UpdateClass[] uc = (UpdateClass[])bf.Deserialize(UpdateFile);
                MemoryStream ms = new MemoryStream();
                bf = new BinaryFormatter();
                bf.Serialize(ms, uc.ToArray());
                ms.Seek(0, SeekOrigin.Begin);

                remoteObject.FullUpdate(ms);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка обновления 1 " + ex.Message, MonitorLog.typelog.Error, true);
            }
            return;
        }

        // обновляет updater broker'a и host'a
        public void UpdateUpdater(Stream UpdaterFile)
        {
            try
            {
                remoteObject.UpdateUpdater(UpdaterFile);
                HostChannel.CloseProxy(remoteObject);
                return;
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка выполнения обновления Updater'а", MonitorLog.typelog.Error, true);
                return;
            }
        }

        // удаляет все резервные копии файлов
        public void RemoveBackupFiles(string WebPath)
        {
            try
            {
                remoteObject.RemoveBackupFiles(WebPath);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка удаления резервной копии " + ex.Message, MonitorLog.typelog.Error, true);
            }
        }

        //восстанавливает из резервной крпии
        public void RestoreFromBackup(string WebPath)
        {
            try
            {
                remoteObject.RestoreFromBackup(WebPath);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка восстановления из резервной копии " + ex.Message, MonitorLog.typelog.Error, true);
            }
        }

        // получает всю историю обновлений
        public Stream GetHistoryFull()
        {
            try
            {
                Stream stream = remoteObject.GetHistoryFull();
                HostChannel.CloseProxy(remoteObject);
                return stream;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получении истории " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
        }

        // получает последние count статусов обновленийиз истории
        public Stream GetHistoryLast(int count)
        {
            try
            {
                Stream stream = remoteObject.GetHistoryLast(count);
                return stream;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получении истории " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
        }

        // проверка соединения
        public int CheckConn()
        {
            int Result = 0;
            try
            {
                Result = remoteObject.CheckConn();
                HostChannel.CloseProxy(remoteObject);
                return Result;
            }
            catch
            {
                return 0;
            }
        }

        public bool ExecSQLFile(Stream SQLStream)
        {
            bool Result = false;
            try
            {
                Result = remoteObject.ExecSQLFile(SQLStream);
                HostChannel.CloseProxy(remoteObject);
                return Result;
            }
            catch
            {
                return false;
            }
        }

        public DateTime GetCurrentMount()
        {
            DateTime Result;
            try
            {
                Result = remoteObject.GetCurrentMount();
                HostChannel.CloseProxy(remoteObject);
                return Result;
            }
            catch
            {
                return new DateTime(1990, 1, 1);
            }
        }

        public bool Replace7zdll(byte[] File7z)
        {
            bool Result;
            try
            {
                Result = remoteObject.Replace7zdll(File7z);
                HostChannel.CloseProxy(remoteObject);
                return Result;
            }
            catch
            {
                return false;
            }
        }

        public bool RestartHosting()
        {
            bool Result;
            try
            {
                Result = remoteObject.RestartHosting();
                HostChannel.CloseProxy(remoteObject);
                return Result;
            }
            catch
            {
                return false;
            }
        }
    }
}

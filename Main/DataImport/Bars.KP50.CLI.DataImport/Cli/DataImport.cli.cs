using System;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

using System.ServiceModel;
using System.Data;
using System.IO;

namespace STCLINE.KP50.Client
{
    public class cli_DataImport : cli_Base, I_DataImport
    {
        //private I_DataImport remoteObject;

        public cli_DataImport(int nzp_server)
            : base()
        {
            //_cli_DataImport(nzp_server);
        }
        IDataImportRemoteObject getRemoteObject()
        {
            return getRemoteObject<IDataImportRemoteObject>(WCFParams.AdresWcfWeb.srvDataImport);
        }

        //private void _cli_DataImport(int nzp_server)
        //{
        //    _RServer zap = MultiHost.GetServer(nzp_server);
        //    string addrHost = "";

        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        //определить параметры доступа
        //        addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvDataImport;
        //        remoteObject = HostChannel.CreateInstance<I_DataImport>(zap.login, zap.pwd, addrHost);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvDataImport;
        //        zap.rcentr = "<локально>";
        //        remoteObject = HostChannel.CreateInstance<I_DataImport>(addrHost);
        //    }

        //    //Попытка открыть канал связи
        //    try
        //    {
        //        ICommunicationObject proxy = remoteObject as ICommunicationObject;
        //        proxy.Open();
        //    }
        //    catch (Exception ex)
        //    {
        //        MonitorLog.WriteLog(
        //            string.Format(
        //                "Ошибка открытия Proxy {0} по адресу {1}. Сервер {2} (Код РЦ {3}) (Код сервера {4}): {5}",
        //                System.Reflection.MethodBase.GetCurrentMethod().Name,
        //                addrHost,
        //                zap.rcentr,
        //                zap.nzp_rc,
        //                nzp_server,
        //                ex.Message),
        //            MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //}

        //~cli_DataImport()
        //{
        //    try
        //    {
        //        if (remoteObject != null) HostChannel.CloseProxy(remoteObject);
        //    }
        //    catch
        //    {
        //    }
        //}

        public ReturnsObjectType<List<UploadingData>> GetUploadingProgress(UploadingData finder)
        {
            ReturnsObjectType<List<UploadingData>> ret = new ReturnsObjectType<List<UploadingData>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUploadingProgress(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUploadingProgress\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUploadingProgress\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }



        public Returns OperateWithFileImported(FilesDisassemble finder, FilesImportedOperations operation)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.OperateWithFileImported(finder, operation);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка OperateWithFileImported\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка OperateWithFileImported\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        //public static void DbGetArms(int nzp_user, ref List<_Arms> Arms)
        //{
        //    DbUserClient db = new DbUserClient();
        //    db.GetArms(nzp_user, ref Arms);
        //    db.Close();
        //}

        //role_pages and role_actions



#warning Старый метод загрузки ЖКУ
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



        public ReturnsObjectType<List<KLADRData>> LoadDataFromKLADR(KLADRFinder finder)
        {
            ReturnsObjectType<List<KLADRData>> ret = new ReturnsObjectType<List<KLADRData>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LoadDataFromKLADR(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadDataFromKLADR\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadDataFromKLADR\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns RefreshKLADRFile(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.RefreshKLADRFile(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UploadKLADR\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UploadKLADR\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public ReturnsObjectType<List<ComparedAreas>> GetComparedArea(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedAreas>> ret = new ReturnsObjectType<List<ComparedAreas>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedArea(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedSupps>> GetComparedSupp(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedSupps>> ret = new ReturnsObjectType<List<ComparedSupps>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedSupp(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedPayer>> GetComparedPayer(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedPayer>> ret = new ReturnsObjectType<List<ComparedPayer>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedPayer(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedVills>> GetComparedMO(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedVills>> ret = new ReturnsObjectType<List<ComparedVills>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedMO(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedMO\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedMO\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedServs>> GetComparedServ(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedServs>> ret = new ReturnsObjectType<List<ComparedServs>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedServ(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedServ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedServ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedMeasures>> GetComparedMeasure(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedMeasures>> ret = new ReturnsObjectType<List<ComparedMeasures>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedMeasure(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedMeasure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedMeasure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParType(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedParType(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedParType\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedParType\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParBlag(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedParTypes>> ret = new ReturnsObjectType<List<ComparedParTypes>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedParBlag(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedParBlag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedParBlag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParGas(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedParGas(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedParGas\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedParGas\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParWater(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedParTypes>> ret = new ReturnsObjectType<List<ComparedParTypes>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedParWater(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedParWater\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedParWater\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedTowns>> GetComparedTown(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedTowns>> ret = new ReturnsObjectType<List<ComparedTowns>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedTown(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedTown\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedTown\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedRajons>> GetComparedRajon(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedRajons>> ret = new ReturnsObjectType<List<ComparedRajons>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedRajon(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedStreets>> GetComparedStreets(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedStreets>> ret = new ReturnsObjectType<List<ComparedStreets>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedStreets(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedStreets\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedStreets\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedHouses>> GetComparedHouse(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedHouses>> ret = new ReturnsObjectType<List<ComparedHouses>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedHouse(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedHouse\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedHouse\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedLS>> GetComparedLS(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedLS>> ret = new ReturnsObjectType<List<ComparedLS>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetComparedLS(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetComparedLS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetComparedLS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkArea(ComparedAreas finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkArea(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkSupp(ComparedSupps finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkSupp(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkPayer(ComparedPayer finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkPayer(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkMO(ComparedVills finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkMO(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkMO\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkMO\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkServ(ComparedServs finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkServ(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkServ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkServ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkParType(ComparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkParType(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkParType\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkParType\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkParBlag(ComparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkParBlag(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkParBlag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkParBlag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkParGas(ComparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkParGas(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkParGas\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkParGas\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkParWater(ComparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkParWater(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkParWater\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkParWater\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkMeasure(ComparedMeasures finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkMeasure(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkMeasure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkMeasure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkTown(ComparedTowns finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkTown(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkTown\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkTown\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkRajon(ComparedRajons finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkRajon(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkStreet(ComparedStreets finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkStreet(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkStreet\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkStreet\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkHouse(ComparedHouses finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkHouse(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkHouse\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkHouse\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType UnlinkLS(ComparedLS finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnlinkLS(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UnlinkLS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UnlinkLS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }




        public ReturnsObjectType<List<UncomparedAreas>> GetUncomparedArea(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedAreas>> ret = new ReturnsObjectType<List<UncomparedAreas>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedArea(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedSupps>> GetUncomparedSupp(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedSupps>> ret = new ReturnsObjectType<List<UncomparedSupps>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedSupp(finder);
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

        public ReturnsObjectType<List<UncomparedPayer>> GetUncomparedPayer(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedPayer>> ret = new ReturnsObjectType<List<UncomparedPayer>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedPayer(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка RGetUncomparedPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedVills>> GetUncomparedMO(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedVills>> ret = new ReturnsObjectType<List<UncomparedVills>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedMO(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedMO\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedMO\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedServs>> GetUncomparedServ(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedServs>> ret = new ReturnsObjectType<List<UncomparedServs>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedServ(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedServ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedServ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParType(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedParType(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedParType\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedParType\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParBlag(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedParBlag(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedParBlag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedParBlag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParGas(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedParGas(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedParGas\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedParGas\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParWater(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedParWater(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedParWater\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedParWater\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedMeasures>> GetUncomparedMeasure(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedMeasures>> ret = new ReturnsObjectType<List<UncomparedMeasures>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedMeasure(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedMeasure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedMeasure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedTowns>> GetUncomparedTown(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedTowns>> ret = new ReturnsObjectType<List<UncomparedTowns>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedTown(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedTown\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedTown\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedRajons>> GetUncomparedRajon(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedRajons>> ret = new ReturnsObjectType<List<UncomparedRajons>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedRajon(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedStreets>> GetUncomparedStreets(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedStreets>> ret = new ReturnsObjectType<List<UncomparedStreets>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedStreets(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedStreets\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedStreets\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedHouses>> GetUncomparedHouse(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedHouses>> ret = new ReturnsObjectType<List<UncomparedHouses>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedHouse(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedHouse\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedHouse\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedLS>> GetUncomparedLS(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedLS>> ret = new ReturnsObjectType<List<UncomparedLS>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUncomparedLS(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUncomparedLS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetUncomparedLS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedAreas>> GetAreaByFilter(UncomparedAreas finder)
        {
            ReturnsObjectType<List<UncomparedAreas>> ret = new ReturnsObjectType<List<UncomparedAreas>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetAreaByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetAreaByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetAreaByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedSupps>> GetSuppByFilter(UncomparedSupps finder)
        {
            ReturnsObjectType<List<UncomparedSupps>> ret = new ReturnsObjectType<List<UncomparedSupps>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSuppByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSuppByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetSuppByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedPayer>> GetPayerByFilter(UncomparedPayer finder)
        {
            ReturnsObjectType<List<UncomparedPayer>> ret = new ReturnsObjectType<List<UncomparedPayer>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetPayerByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPayerByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetPayerByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedVills>> GetMOByFilter(UncomparedVills finder)
        {
            ReturnsObjectType<List<UncomparedVills>> ret = new ReturnsObjectType<List<UncomparedVills>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetMOByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetMOByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetMOByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedServs>> GetServByFilter(UncomparedServs finder)
        {
            ReturnsObjectType<List<UncomparedServs>> ret = new ReturnsObjectType<List<UncomparedServs>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetServByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetServByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetServByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParTypeByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetParTypeByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetParTypeByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetParTypeByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParBlagByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetParBlagByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetParBlagByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetParBlagByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParGasByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetParGasByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetParGasByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetParGasByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParWaterByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetParWaterByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetParWaterByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetParWaterByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedMeasures>> GetMeasureByFilter(UncomparedMeasures finder)
        {
            ReturnsObjectType<List<UncomparedMeasures>> ret = new ReturnsObjectType<List<UncomparedMeasures>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetMeasureByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetMeasureByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetMeasureByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedTowns>> GetTownByFilter(UncomparedTowns finder)
        {
            ReturnsObjectType<List<UncomparedTowns>> ret = new ReturnsObjectType<List<UncomparedTowns>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetTownByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetTownByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetTownByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedRajons>> GetRajonByFilter(UncomparedRajons finder)
        {
            ReturnsObjectType<List<UncomparedRajons>> ret = new ReturnsObjectType<List<UncomparedRajons>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetRajonByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetRajonByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetRajonByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedStreets>> GetStreetsByFilter(UncomparedStreets finder)
        {
            ReturnsObjectType<List<UncomparedStreets>> ret = new ReturnsObjectType<List<UncomparedStreets>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetStreetsByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetStreetsByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetStreetsByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedHouses>> GetHouseByFilter(UncomparedHouses finder)
        {
            ReturnsObjectType<List<UncomparedHouses>> ret = new ReturnsObjectType<List<UncomparedHouses>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetHouseByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetHouseByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetHouseByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedLS>> GetLsByFilter(UncomparedLS finder)
        {
            ReturnsObjectType<List<UncomparedLS>> ret = new ReturnsObjectType<List<UncomparedLS>>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetLsByFilter(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetLsByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetLsByFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType ChangeTownForRajon(UncomparedRajons finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.ChangeTownForRajon(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ChangeTownForRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка ChangeTownForRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkArea(UncomparedAreas finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkArea(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        public Returns LinkHouseOnly(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkHouseOnly(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkHouseOnly\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkHouseOnly\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkSupp(UncomparedSupps finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkSupp(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkPayer(UncomparedPayer finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkPayer(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkMO(UncomparedVills finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkMO(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkMO\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkMO\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkServ(UncomparedServs finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkServ(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkServ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkServ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkParType(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkParType(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkParType\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkParType\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkParBlag(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkParBlag(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkParBlag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkParBlag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkParGas(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkParGas(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkParGas\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkParGas\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkParWater(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkParWater(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkParWater\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkParWater\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkMeasure(UncomparedMeasures finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkMeasure(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkMeasure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkMeasure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkTown(UncomparedTowns finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkTown(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkTown\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkTown\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkRajon(UncomparedRajons finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkRajon(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkNzpStreet(UncomparedStreets finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkNzpStreet(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkNzpStreet\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkNzpStreet\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkNzpDom(UncomparedHouses finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkNzpDom(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkNzpDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkNzpDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType LinkLS(UncomparedLS finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkLS(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkLS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkLS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType AddNewArea(UncomparedAreas finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewArea(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNewArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType AddNewSupp(UncomparedSupps finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewSupp(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNewSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType AddNewPayer(UncomparedPayer finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewPayer(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNewPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType AddNewMO(UncomparedVills finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewMO(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNewMO\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewMO\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType AddNewServ(UncomparedServs finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewServ(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNewServ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewServ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType AddNewParType(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewParType(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNewParType\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewParType\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType AddNewParBlag(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewParBlag(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNewParBlag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewParBlag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType AddNewParGas(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewParGas(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNewParGas\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewParGas\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType AddNewParWater(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewParWater(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNewParWater\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewParWater\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType AddNewMeasure(UncomparedMeasures finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewMeasure(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNewMeasure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewMeasure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType AddNewRajon(UncomparedRajons finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewRajon(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNewRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType AddNewStreet(UncomparedStreets finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewStreet(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNewStreet\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewStreet\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType AddNewHouse(UncomparedHouses finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewHouse(finder, selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNewHouse\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddNewHouse\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType DbSaveFileToDisassembly(FilesImported finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DbSaveFileToDisassembly(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DbSaveFileToDisassembly\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка DbSaveFileToDisassembly\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns AddAllHouse(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddAllHouse(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddAllHouse\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddAllHouse\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }



        public Returns LinkLSAutom(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkLSAutom(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkLSAutom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkLSAutom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public Returns LinkEmptyStreetAutom(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkEmptyStreetAutom(finder);
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

        
        public Returns StopRefresh()
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.StopRefresh();
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка StopRefresh\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка StopRefresh\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        public Returns LinkStreetAutom(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkStreetAutom(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkStreetAutom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkStreetAutom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns LinkPayerWithAdd(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkPayerWithAdd(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkPayerWithAdd\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkPayerWithAdd\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns LinkPayerAutom(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkPayerAutom(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkPayerAutom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkPayerAutom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns LinkParAutomWithAdd(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkParAutomWithAdd(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkParAutomWithAdd\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkParAutomWithAdd\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns LinkParamsAutom(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkParamsAutom(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkParamsAutom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkParamsAutom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns LinkRajonAutom(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkRajonAutom(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkRajonAutom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkRajonAutom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns LinkServiceAuto(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LinkServiceAuto(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LinkRajonAutom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LinkRajonAutom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns MakePacks(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.MakePacks(finder);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка MakePacks" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns UsePreviousLinks(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UsePreviousLinks(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UsePreviousLinks\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UsePreviousLinks\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns MakeReportOfLoad(FilesImported finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.MakeReportOfLoad(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка MakeReportOfLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка MakeReportOfLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<int> GetFilesIsNotExistAct(FilesImported finder, out Returns ret) {
            ret = new Returns();
            var listInt = new List<int>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    listInt = ro.GetFilesIsNotExistAct(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFilesIsNotExistAct\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetFilesIsNotExistAct\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listInt;
        }

        public ReturnsObjectType<List<FilesImported>> GetFiles(FilesImported finder)
        {
            ReturnsObjectType<List<FilesImported>> ret = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFiles(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFiles\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetFiles\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns GetNzpFileLoad(FilesImported finder)
        {
            Returns ret = Utils.InitReturns(); ;
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetNzpFileLoad(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetNzpFileLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetNzpFileLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns MakeUniquePayer(List<int> selectedFiles)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.MakeUniquePayer(selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка MakeUniquePayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка MakeUniquePayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns MakeNonUniquePayer(List<int> selectedFiles)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.MakeNonUniquePayer(selectedFiles);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка MakeUniquePayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка MakeUniquePayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
    }
}
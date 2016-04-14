using System;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;
using System.Collections;
using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

using STCLINE.KP50.Utility;

namespace STCLINE.KP50.Client
{
    public class cli_Prm : I_Prm
    {
        I_Prm remoteObject;

        public cli_Prm(int nzp_server)
            : base()
        {
            _cli_Prm(nzp_server);
        }

        void _cli_Prm(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvPrm;
                remoteObject = HostChannel.CreateInstance<I_Prm>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvPrm;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Prm>(addrHost);
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

        ~cli_Prm()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        List<Param> paramSpis = new List<Param>();
        List<Prm> prmSpis = new List<Prm>();
        List<LightPrm> shortPrmSpis = new List<LightPrm>();

        //----------------------------------------------------------------------
        public List<Prm> GetPrm(Prm finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                prmSpis = remoteObject.GetPrm(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return prmSpis;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetPrm(" + oper.ToString() + ") " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }



        //----------------------------------------------------------------------
        public List<LightPrm> GetShortPrm(Prm finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                shortPrmSpis = remoteObject.GetShortPrm(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return shortPrmSpis;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetPrm(" + oper.ToString() + ") " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public Returns SavePrmArea(Prm finder)
        //----------------------------------------------------------------------
        {
            Returns ret;
            try
            {
                ret = remoteObject.SavePrmArea(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = string.Empty;
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SavePrmArea " + err, MonitorLog.typelog.Error, 2, 100, true);
                return new Returns(false, ex.Message);
            }
        }

        //----------------------------------------------------------------------
        public string GetSetRemarkForGeu(Prm finder, bool edit, string rem, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                string remark = remoteObject.GetSetRemarkForGeu(finder, edit, rem, out ret);
                HostChannel.CloseProxy(remoteObject);
                return remark;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetRemarkForGeu " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public Param FindPrmInfo(Prm finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                Param prm = remoteObject.FindPrmInfo(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return prm;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FindPrmInfo() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public Returns CopyLsParams(Ls finderFrom, Ls finderTo)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.CopyLsParams(finderFrom, finderTo);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка при копировании параметров л/с";

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CopyLsParams() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return new Returns(false, ex.Message);
            }
        }
        /// <summary> Сохранить или удалить значения параметра
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        //----------------------------------------------------------------------
        public Returns SavePrm(Param finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SavePrm(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка при сохранении параметров для группы л/с";

                MonitorLog.WriteLog("Ошибка SavePrm() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return new Returns(false, ex.Message);
            }
        }

        public Returns SaveResY(Res_y finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveResY(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка при сохранении значения параметра";

                MonitorLog.WriteLog("Ошибка SaveResY() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return new Returns(false, ex.Message);
            }
        }

        public Returns SaveResolution(Resolution finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveResolution(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка при сохранении";

                MonitorLog.WriteLog("Ошибка SaveResolution() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return new Returns(false, ex.Message);
            }
        }

        public Returns SavePrmName(Prm finder, enSrvOper oper)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SavePrmName(finder, oper);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка при сохранении параметра";

                MonitorLog.WriteLog("Ошибка SavePrmName() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return new Returns(false, ex.Message);
            }
        }
        //----------------------------------------------------------------------
        public Prm FindPrmValue(Prm finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                Prm prm = remoteObject.FindPrmValue(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return prm;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FindPrmValue() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Res_y> LoadSpravValueForPrm(Param finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Res_y> list = remoteObject.LoadSpravValueForPrm(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadSpravValueForPrm() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Resolution> GetListResolution(Resolution finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Resolution> list = new List<Resolution>();
            try
            {
                if (finder.rows > 0) list = remoteObject.GetListResolution(finder, out ret);
                else
                {
                    finder.rows = 500;
                    for (ret = new Returns(true, null, finder.skip + 1); finder.skip < ret.tag && ret.result; finder.skip += finder.rows)
                    {
                        IList<Resolution> tmplist = remoteObject.GetListResolution(finder, out ret);
                        if (tmplist != null) list.AddRange(tmplist);
                    }

                }
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetListResolution() " + err + " " + ex.StackTrace, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<PrmTypes> NormParamValues(Param finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<PrmTypes> list = new List<PrmTypes>();
            try
            {
                list = remoteObject.NormParamValues(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка NormParamValues() " + err + " " + ex.StackTrace, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------

        public List<PrmTypes> LoadPrmTypes(PrmTypes finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<PrmTypes> list = remoteObject.LoadPrmTypes(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadPrmTypes() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<Prm> FindPrmValues(Prm finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Prm> prm = remoteObject.FindPrmValues(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return prm;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FindPrmValues() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<Param> LoadParamsWithNumer(Prm finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Param> prm = remoteObject.LoadParamsWithNumer(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return prm;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadParamsWithNumer() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public Returns DeleteLs(Ls finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.DeleteLs(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка при удалении л/с";

                MonitorLog.WriteLog("Ошибка DeleteLs() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return new Returns(false, ex.Message);
            }
        }

        //----------------------------------------------------------------------
        public List<Prm> GetNorms(Prm prm, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Prm> prms = remoteObject.GetNorms(prm, out ret);
                HostChannel.CloseProxy(remoteObject);
                return prms;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetNorms() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }


        //----------------------------------------------------------------------
        public List<Prm> GetPeriod(Prm prm, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Prm> prms = remoteObject.GetPeriod(prm, out ret);
                HostChannel.CloseProxy(remoteObject);
                return prms;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetPeriod() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }



        //----------------------------------------------------------------------
        public bool DeletePeriod(Prm prm, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                bool flag = remoteObject.DeletePeriod(prm, out ret);
                HostChannel.CloseProxy(remoteObject);
                return flag;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка DeletePeriod() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
        }

        //----------------------------------------------------------------------
        public DynamicTable GetTableData(Prm prm, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                DynamicTable res_data = new DynamicTable();
                res_data = remoteObject.GetTableData(prm, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res_data;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetTableData() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public bool UpdateTableData(Prm prm, ArrayList list, int nzp_y, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                bool flag = remoteObject.UpdateTableData(prm, list, nzp_y, out ret);
                HostChannel.CloseProxy(remoteObject);
                return flag;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UpdateTableData() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
        }

        //----------------------------------------------------------------------
        public List<Prm> GetKvarPrmList(Prm finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Prm> list = remoteObject.GetKvarPrmList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetKvarPrmList()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<NormTreeView> GetServForNorm(out Returns ret, int nzp_wp, bool showOld)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<NormTreeView> list = remoteObject.GetServForNorm(out ret, nzp_wp, showOld);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetServForNorm()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<NormParam> GetAddNormParam(out Returns ret, int id)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<NormParam> list = remoteObject.GetAddNormParam(out ret, id);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetAddNormParam()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public Returns SaveNormFirstStage(Norm norm)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveNormFirstStage(norm);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveNormFirstStage()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        //----------------------------------------------------------------------
        public Returns SaveNormParamStatus(int normTypeId)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveNormParamStatus(normTypeId);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveNormParamStatus()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }


        //----------------------------------------------------------------------
        public Returns SaveParamSecondStage(NormTypesSign finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveParamSecondStage(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveParamSecondStage()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        //----------------------------------------------------------------------
        public List<PrmTypes> GetMeasuresForNorm(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<PrmTypes> list = remoteObject.GetMeasuresForNorm(out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetMeasuresForNorm()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<PrmTypes> GetKindNorm(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<PrmTypes> list = remoteObject.GetKindNorm(out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetKindNorm()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<PrmTypes> GetServNorm(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<PrmTypes> list = remoteObject.GetServNorm(out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetServNorm()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<PrmTypes> GetParamNorm(int TypePrm, int NormtypeId, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<PrmTypes> list = remoteObject.GetParamNorm(TypePrm, NormtypeId, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetParamLsNorm()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<PrmTypes> GetValueTypesNorm(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<PrmTypes> list = remoteObject.GetValueTypesNorm(out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetValueTypesNorm()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public Norm GetNormParam(out Returns ret, int id_norm)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                Norm norm = remoteObject.GetNormParam(out ret, id_norm);
                HostChannel.CloseProxy(remoteObject);
                return norm;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetNormParam()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return new Norm();
            }
        }

        //----------------------------------------------------------------------
        public Norm GetNormParamValuesOnLoadPage(out Returns ret, int id_norm)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                Norm norm = remoteObject.GetNormParamValuesOnLoadPage(out ret, id_norm);
                HostChannel.CloseProxy(remoteObject);
                return norm;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetNormParamValuesOnLoadPage()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return new Norm();
            }
        }

        //----------------------------------------------------------------------
        public List<NormParamValue> GetNormParamValues(out Returns ret, int nzp_prm, int norm_type_id)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<NormParamValue> list = remoteObject.GetNormParamValues(out ret, nzp_prm, norm_type_id);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetNormParamValues()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return new List<NormParamValue>();
            }
        }
        //----------------------------------------------------------------------
        public Param FindParam(Param finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                Param param = remoteObject.FindParam(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return param;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка FindParam()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public void UpdateTarifCalculation(List<Prm> finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.UpdateTarifCalculation(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UpdatePrmTarifCalculation()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
        }


        public List<NormParamCombination> GetCombinations(NormFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<NormParamCombination> res = new List<NormParamCombination>();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetCombinations(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetCombinations()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return res;
            }
        }

        public DataTable GetNormatives(NormFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            DataTable res = new DataTable();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetNormatives(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetNormatives()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return res;
            }
        }

        public Returns SaveNormatives(NormFinder finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveNormatives(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveNormatives()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns InsertNewNormValues(NormFinder finder, DataSet NewNorms)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.InsertNewNormValues(finder, NewNorms);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка InsertNewNormValues()" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public NormParam GetTypePrmIdByNzpPrm(string nzp_prm, int normId, out Returns ret)
        {
            NormParam np = new NormParam();
            ret = Utils.InitReturns();
            try
            {
                np = remoteObject.GetTypePrmIdByNzpPrm(nzp_prm, normId, out ret);
                HostChannel.CloseProxy(remoteObject);
                return np;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + 
                    (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return np;
        }

        public Dictionary<TypeTarif, PrmTarifs> GetGroupedTarifsData(PrmTarifFinder finder, out Returns ret)
        {
            var res = new Dictionary<TypeTarif, PrmTarifs>();
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.GetGroupedTarifsData(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public Returns SetGroupedTarifsData(Dictionary<TypeTarif, PrmTarifs> newTarifs,
            PrmTarifFinder finder)
        {
            var ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SetGroupedTarifsData(newTarifs, finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<long> GetListLsByTarif(PrmTarifFinder finder, TypeTarif type, Tarif tarif, out Returns ret)
        {
            ret = Utils.InitReturns();
            var res = new List<long>();
            try
            {
                res = remoteObject.GetListLsByTarif(finder, type, tarif, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

    }
}

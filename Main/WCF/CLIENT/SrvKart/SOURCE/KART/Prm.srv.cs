using System;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;
using System.Collections;
using System.Collections.Generic;
using Bars.KP50.DB.Parameters.Source;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Utility;
using STCLINE.KP50.Client;

namespace STCLINE.KP50.Server
{
    //----------------------------------------------------------------------
    public class srv_Prm : srv_Base, I_Prm //сервис Параметров
    //----------------------------------------------------------------------
    {
        List<Param> paramSpis = new List<Param>();
        List<Prm> prmSpis = new List<Prm>();
        List<LightPrm> shortPrmSpis = new List<LightPrm>();

        //----------------------------------------------------------------------
        public List<Prm> GetPrm(Prm finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            prmSpis.Clear();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                prmSpis = cli.GetPrm(finder, oper, out ret);
            }
            else
            {
                DbParameters dbparam = new DbParameters();
                try
                {
                    switch (oper)
                    {
                        case enSrvOper.SrvFind:

                            dbparam.FindPrm(finder, out ret);
                            if (ret.result)
                                prmSpis = dbparam.GetPrm(finder, out ret);
                            else
                                prmSpis = null;
                            dbparam.Close();
                            break;
                        case enSrvOper.SrvGet:
                            prmSpis = dbparam.GetPrm(finder, out ret);
                            dbparam.Close();
                            break;
                        case enSrvOper.SrvLoad:
                            prmSpis = dbparam.LoadParams(finder, out ret);
                            dbparam.Close();
                            break;
                        case enSrvOper.srvFindPrmTarif:
                            prmSpis = dbparam.FindPrmTarif(finder, out ret);
                            dbparam.Close();
                            break;
                        case enSrvOper.srvFindPrmTarifCalculation:
                            prmSpis = dbparam.FindPrmTarifCalculation(finder, out ret);
                            dbparam.Close();
                            break;
                        case enSrvOper.srvFindPrmCalculation:
                            prmSpis = dbparam.FindPrmCalculation(finder, out ret);
                            dbparam.Close();
                            break;
                        case enSrvOper.srvFindPrmCalculationFormuls:
                            prmSpis = dbparam.FindPrmCalculationFormuls(finder, out ret);
                            dbparam.Close();
                            break;
                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения параметров";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetPrm(" + oper.ToString() + ") \n  " + ex.Message,
                            MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    dbparam.Close();
                }
            }
            return prmSpis;
        }

        public List<LightPrm> GetShortPrm(Prm finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            shortPrmSpis.Clear();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                shortPrmSpis = cli.GetShortPrm(finder, oper, out ret);
            }
            else
            {
                DbParameters dbparam = new DbParameters();
                try
                {
                    switch (oper)
                    {
                        //case enSrvOper.SrvFind:
                        //case enSrvOper.SrvGet:
                        case enSrvOper.SrvLoad:
                            shortPrmSpis = dbparam.LoadShortParams(finder, out ret);
                            dbparam.Close();
                            break;
                        //case enSrvOper.srvFindPrmTarif:
                        //case enSrvOper.srvFindPrmTarifCalculation:
                        //case enSrvOper.srvFindPrmCalculation:
                        //case enSrvOper.srvFindPrmCalculationFormuls:
                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения параметров";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetPrm(" + oper.ToString() + ") \n  " + ex.Message,
                            MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    dbparam.Close();
                }
            }
            return shortPrmSpis;
        }

        //----------------------------------------------------------------------
        public Returns SavePrmArea(Prm finder)
            //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            var dbparam = new DbParameters();
            try
            {
                ret = dbparam.SavePrmArea(finder);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug)
                    MonitorLog.WriteLog("Ошибка SavePrmArea() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {
                dbparam.Close();
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public Param FindPrmInfo(Prm finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                Param param = cli.FindPrmInfo(finder, out ret);
                return param;
            }
            else
            {
                try
                {
                    Param param;
                    using (var dbparam = new DbParameters())
                    {
                        param = dbparam.FindPrmInfo(finder, out ret);
                    }
                    return param;
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка FindPrmInfo() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
            }
        }


        //----------------------------------------------------------------------
        public Returns CopyLsParams(Ls finderFrom, Ls finderTo)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {
                ret = dbparam.CopyLsParams(finderFrom, finderTo);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug)
                    MonitorLog.WriteLog("Ошибка CopyLsParams() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100,
                        true);
            }
            finally
            {
                dbparam.Close();
            }
            return ret;
        }

        public List<Res_y> LoadSpravValueForPrm(Param finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {

                List<Res_y> list = dbparam.LoadSpravValueForPrm(finder, out ret);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadSpravValueForPrm() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
            finally
            {
                dbparam.Close();
            }
        }

        public List<Resolution> GetListResolution(Resolution finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {

                List<Resolution> list = dbparam.GetListResolution(finder, out ret);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetListResolution() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
            finally
            {
                dbparam.Close();
            }
        }

        public List<PrmTypes> NormParamValues(Param finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {
                List<PrmTypes> list = dbparam.NormParamValues(finder, out ret);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка NormParamValues() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
            finally
            {
                dbparam.Close();
            }
        }

        public List<PrmTypes> LoadPrmTypes(PrmTypes finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {
                List<PrmTypes> list = dbparam.LoadPrmTypes(finder, out ret);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadPrmTypes() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
            finally
            {
                dbparam.Close();
            }
        }


        //----------------------------------------------------------------------
        public Returns SavePrm(Param finder)
        //----------------------------------------------------------------------
        {
            try
            {
                Returns ret = Utils.InitReturns();
                using (DbParameters dbparam = new DbParameters())
                {
                    ret = dbparam.SavePrm(finder);
                }

                if (ret.result)
                {
                    if (finder.nzp_prm == (int)ParamIds.LsParams.State)
                    {
                        if (Utils.GetParams(finder.prms, Constants.page_group_ls_prm_dom))
                        {
                            //Групповая операция по списку домов
                            using (DbAdres dba = new DbAdres())
                            {
                                //Групповая операция по списку лицевых счетов
                                Returns ret2 = dba.RefreshListDomLsStates(finder.nzp_user);
                            }
                        }
                        else if (finder.nzp > 0)
                        {
                            //Операция с одним лицевым счетом
                            using (DbAdres dba = new DbAdres())
                            {
                                Returns ret2 = dba.RefreshLsState(finder.pref, Convert.ToInt32(finder.nzp), finder.nzp_user);
                            }
                        }
                        else
                        {
                            using (DbAdres dba = new DbAdres())
                            {
                                //Групповая операция по списку лицевых счетов
                                Returns ret2 = dba.RefreshListLsStates(finder.nzp_user);
                            }
                        }
                    }
                }

                return ret;
            }
            catch (Exception ex)
            {
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка SavePrm() \n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return new Returns(false, "Ошибка вызова функции получения параметров" + (Constants.Viewerror ? ": " + ex.Message : ""));
            }
        }

        public Returns SaveResolution(Resolution finder)
        {
            try
            {
                Returns ret = Utils.InitReturns();
                using (DbParameters dbparam = new DbParameters())
                {
                    ret = dbparam.SaveResolution(finder);
                }
                return ret;
            }
            catch (Exception ex)
            {
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveResolution() \n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return new Returns(false, "Ошибка вызова функции сохранения " + (Constants.Viewerror ? ": " + ex.Message : ""));
            }
        }

        public Returns SaveResY(Res_y finder)
        //----------------------------------------------------------------------
        {
            try
            {
                Returns ret = Utils.InitReturns();
                using (DbParameters dbparam = new DbParameters())
                {
                    ret = dbparam.SaveResY(finder);
                }
                return ret;
            }
            catch (Exception ex)
            {
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveResY() \n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return new Returns(false, "Ошибка вызова функции сохранения значения параметра" + (Constants.Viewerror ? ": " + ex.Message : ""));
            }
        }

        public Returns SavePrmName(Prm finder, enSrvOper oper)
        {
            DbParameters dbparam = new DbParameters();
            try
            {
                Returns ret = Utils.InitReturns();
                switch (oper)
                {
                    case enSrvOper.SrvEdit:
                    {
                        ret = dbparam.SavePrmName(finder);
                        break;
                    }

                    case enSrvOper.srvDelete:
                    {
                        ret = dbparam.DeletePrmName(finder);
                        break;
                    }
                }


                return ret;
            }
            catch (Exception ex)
            {
                if (Constants.Debug)
                    MonitorLog.WriteLog("Ошибка SavePrmName() \n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return new Returns(false,
                    "Ошибка вызова функции сохранения параметров" + (Constants.Viewerror ? ": " + ex.Message : ""));
            }
            finally
            {
                dbparam.Close();
            }
        }

        //----------------------------------------------------------------------
        public Prm FindPrmValue(Prm finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {
                Prm prm = dbparam.FindPrmValue(finder, out ret);
                return prm;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug)
                    MonitorLog.WriteLog("Ошибка FindPrmValue() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100,
                        true);
                return null;
            }
            finally
            {
                dbparam.Close();
            }
        }
        //----------------------------------------------------------------------
        public List<Prm> FindPrmValues(Prm finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {
                List<Prm> prm = dbparam.FindPrmValues(finder, out ret);
                return prm;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug)
                    MonitorLog.WriteLog("Ошибка FindPrmValues() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100,
                        true);
                return null;
            }
            finally
            {
                dbparam.Close();
            }
        }
        //----------------------------------------------------------------------
        public List<Param> LoadParamsWithNumer(Prm finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {

                List<Param> list = null;
                if (finder.prm_num == ParamNums.General10)
                    list = dbparam.GetParamsByPrmNum(finder, out ret);
                else
                    list = dbparam.LoadParamsWithNumer(finder, out ret);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug)
                    MonitorLog.WriteLog("Ошибка LoadParamsWithNumer() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                        100, true);
                return null;
            }
            finally
            {
                dbparam.Close();
            }
        }
        //----------------------------------------------------------------------
        public Returns DeleteLs(Ls finder)
        //----------------------------------------------------------------------
        {
            DbParameters dbparam = new DbParameters();

            try
            {
                Returns ret = dbparam.DeleteLs(finder);
                return ret;
            }
            catch (Exception ex)
            {
                if (Constants.Debug)
                    MonitorLog.WriteLog("Ошибка DeleteLs() \n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return new Returns(false,
                    "Ошибка вызова функции удаления лицевого счета" + (Constants.Viewerror ? ": " + ex.Message : ""));
            }
            finally
            {
                dbparam.Close();
            }
        }

        //----------------------------------------------------------------------
        public List<Prm> GetNorms(Prm prm, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {
                List<Prm> prms = dbparam.GetNorms(prm, out ret);
                return prms;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug)
                    MonitorLog.WriteLog("Ошибка GetNorms() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
            finally
            {
                dbparam.Close();
            }
        }

        //----------------------------------------------------------------------
        public List<Prm> GetPeriod(Prm prm, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();

            try
            {
                List<Prm> prms = dbparam.GetPeriod(prm, out ret);
                return prms;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug)
                    MonitorLog.WriteLog("Ошибка GetPeriod() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
            finally
            {
                dbparam.Close();

            }
        }


        //----------------------------------------------------------------------
        public bool DeletePeriod(Prm prm, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {
                bool flag = dbparam.DeletePeriod(prm, out ret);
                return flag;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug)
                    MonitorLog.WriteLog("Ошибка DeletePeriod() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100,
                        true);
                return false;
            }
            finally
            {
                dbparam.Close();
            }
        }

        //----------------------------------------------------------------------
        public DynamicTable GetTableData(Prm prm, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {
                DynamicTable res_data = new DynamicTable();
                res_data.table = dbparam.GetTableData(prm, out ret);
                return res_data;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetTableData() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
            finally
            {
                dbparam.Close();
            }
        }

        //----------------------------------------------------------------------
        public bool UpdateTableData(Prm prm, ArrayList list, int nzp_y, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {
                bool flag = dbparam.UpdateTableData(prm, list, nzp_y, out ret);
                return flag;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка UpdateTableData() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
            finally
            {
                dbparam.Close();
            }
        }

        //----------------------------------------------------------------------
        public List<Prm> GetKvarPrmList(Prm finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Prm> list = null;

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    list = cli.GetKvarPrmList(finder, out ret);
                }
                else
                {
                    ReturnsObjectType<DataTable> r = new ClassDB().RunSqlAction(finder, new DbParameters().GetKvarPrmList);
                    ret = r.GetReturns();
                    if (r.returnsData != null)
                        list = OrmConvert.ConvertDataRows<Prm>(r.returnsData.Rows, DbParameters.ToPrmValue);
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return list;
        }

        //----------------------------------------------------------------------
        public List<NormTreeView> GetServForNorm(out Returns ret, int nzp_wp, bool showOld)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<NormTreeView> list = new List<NormTreeView>();

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    list = cli.GetServForNorm(out ret, nzp_wp, showOld);
                }
                else
                {

                    using (var dbparam = new DbParameters())
                    {
                        list = dbparam.GetServForNorm(out ret, nzp_wp, showOld);
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return list;
        }

        //----------------------------------------------------------------------
        public List<NormParam> GetAddNormParam(out Returns ret, int id)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<NormParam> list = new List<NormParam>();

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    list = cli.GetAddNormParam(out ret, id);
                }
                else
                {

                    using (var dbparam = new DbParameters())
                    {
                        list = dbparam.GetAddNormParam(out ret, id);
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return list;
        }

        //----------------------------------------------------------------------
        public Returns SaveNormFirstStage(Norm norm)
        //----------------------------------------------------------------------
        {
            Returns ret = new Returns(false);

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    ret = cli.SaveNormFirstStage(norm);
                }
                else
                {

                    using (var dbparam = new DbParameters())
                    {
                        ret = dbparam.SaveNormFirstStage(norm);
                    }
                    return ret;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return ret;
        }


        //----------------------------------------------------------------------
        public Returns SaveNormParamStatus(int normTypeId)
        //----------------------------------------------------------------------
        {
            Returns ret = new Returns(false);

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    ret = cli.SaveNormParamStatus(normTypeId);
                }
                else
                {

                    using (DbParameters dbparam = new DbParameters())
                    {
                        ret = dbparam.SaveNormParamStatus(normTypeId);
                    }
                    return ret;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return ret;
        }


        //----------------------------------------------------------------------
        public Returns SaveParamSecondStage(NormTypesSign finder)
        //----------------------------------------------------------------------
        {
            Returns ret = new Returns(false);

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    ret = cli.SaveParamSecondStage(finder);
                }
                else
                {

                    using (DbParameters dbparam = new DbParameters())
                    {
                        ret = dbparam.SaveParamSecondStage(finder);
                    }
                    return ret;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public List<PrmTypes> GetMeasuresForNorm(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<PrmTypes> list = new List<PrmTypes>();

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    list = cli.GetMeasuresForNorm(out ret);
                }
                else
                {

                    using (var dbparam = new DbParameters())
                    {
                        list = dbparam.GetMeasuresForNorm(out ret);
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return list;
        }

        //----------------------------------------------------------------------
        public List<PrmTypes> GetKindNorm(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<PrmTypes> list = new List<PrmTypes>();

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    list = cli.GetKindNorm(out ret);
                }
                else
                {

                    using (DbParameters dbparam = new DbParameters())
                    {
                        list = dbparam.GetKindNorm(out ret);
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return list;
        }

        //----------------------------------------------------------------------
        public List<PrmTypes> GetServNorm(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<PrmTypes> list = new List<PrmTypes>();

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    list = cli.GetServNorm(out ret);
                }
                else
                {

                    using (DbParameters dbparam = new DbParameters())
                    {
                        list = dbparam.GetServNorm(out ret);
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return list;
        }

        //----------------------------------------------------------------------
        public List<PrmTypes> GetParamNorm(int TypePrm, int NormTypeId, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<PrmTypes> list = new List<PrmTypes>();

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    list = cli.GetParamNorm(TypePrm, NormTypeId, out ret);
                }
                else
                {

                    using (var dbparam = new DbParameters())
                    {
                        list = dbparam.GetParamNorm(TypePrm, NormTypeId, out ret);
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return list;
        }

        //----------------------------------------------------------------------
        public List<PrmTypes> GetValueTypesNorm(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<PrmTypes> list = new List<PrmTypes>();

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    list = cli.GetValueTypesNorm(out ret);
                }
                else
                {

                    using (var dbparam = new DbParameters())
                    {
                        list = dbparam.GetValueTypesNorm(out ret);
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return list;
        }

        //----------------------------------------------------------------------
        public Norm GetNormParam(out Returns ret, int id_norm)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Norm norm = new Norm();

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    norm = cli.GetNormParam(out ret, id_norm);
                }
                else
                {

                    using (DbParameters dbparam = new DbParameters())
                    {
                        norm = dbparam.GetNormParam(out ret, id_norm);
                    }
                    return norm;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return norm;
        }

        //----------------------------------------------------------------------

        public Norm GetNormParamValuesOnLoadPage(out Returns ret, int id_norm)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Norm norm = new Norm();

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    norm = cli.GetNormParamValuesOnLoadPage(out ret, id_norm);
                }
                else
                {

                    using (var dbparam = new DbParameters())
                    {
                        norm = dbparam.GetNormParamValuesOnLoadPage(out ret, id_norm);
                    }
                    return norm;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return norm;
        }

        //----------------------------------------------------------------------
        public List<NormParamValue> GetNormParamValues(out Returns ret, int nzp_prm, int norm_type_id)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<NormParamValue> norm = new List<NormParamValue>();

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    norm = cli.GetNormParamValues(out ret, nzp_prm, norm_type_id);
                }
                else
                {

                    using (var dbparam = new DbParameters())
                    {
                        norm = dbparam.GetNormParamValues(out ret, nzp_prm, norm_type_id);
                    }
                    return norm;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return norm;
        }

        //----------------------------------------------------------------------
        public Param FindParam(Param finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Param param = null;

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    param = cli.FindParam(finder, out ret);
                }
                else
                {
                    ReturnsObjectType<Param> r = new ClassDB().RunSqlAction(finder, new DbParameters().FindParam);
                    ret = r.GetReturns();
                    param = r.returnsData;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return param;
        }

        //----------------------------------------------------------------------
        public string GetSetRemarkForGeu(Prm finder, bool edit, string rem, out Returns ret)
        //----------------------------------------------------------------------       
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {

                string remark = dbparam.GetSetRemarkForGeu(finder, edit, rem, out ret);
                return remark;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug)
                    MonitorLog.WriteLog("Ошибка DeletePeriod() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100,
                        true);
                return "";
            }
            finally
            {
                dbparam.Close();
            }

        }


        public void UpdateTarifCalculation(List<Prm> finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            DbParameters dbparam = new DbParameters();
            try
            {

                switch (oper)
                {
                    case enSrvOper.srvUpdateTarifCalculation:
                    {
                        dbparam.UpdatePrmTarifCalculation(finder, out ret);
                        break;
                    }

                    case enSrvOper.srvAddPrmCalculation:
                    {
                        dbparam.AddPrmCalculation(finder, out ret);
                        break;
                    }
                    case enSrvOper.srvAddPrmTarifCalculation:
                    {
                        dbparam.AddPrmTarifCalculation(finder, out ret);
                        break;
                    }
                    case enSrvOper.sqrDelPrmCalculation:
                    {
                        dbparam.DelPrmCalculation(finder, out ret);
                        break;
                    }
                    case enSrvOper.sqrDelPrmTarifCalculation:
                    {
                        dbparam.DelPrmTarifCalculation(finder, out ret);
                        break;
                    }
                }


            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug)
                    MonitorLog.WriteLog("Ошибка UpdatePrmTarifCalculation() \n  " + ex.Message, MonitorLog.typelog.Error,
                        2, 100, true);
            }
            finally
            {
                dbparam.Close();
            }
        }



        public List<NormParamCombination> GetCombinations(NormFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<NormParamCombination> res = new List<NormParamCombination>();
            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    res = cli.GetCombinations(finder, out  ret);
                }
                else
                {

                    using (DbParameters dbparam = new DbParameters())
                    {
                        res = dbparam.GetCombinations(finder, out ret);
                    }
                    return res;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return res;
        }

        public DataTable GetNormatives(NormFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            DataTable res = new DataTable();
            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    res = cli.GetNormatives(finder, out  ret);
                }
                else
                {

                    using (var dbparam = new DbParameters())
                    {
                        res = dbparam.GetNormatives(finder, out ret);
                    }
                    return res;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return res;
        }

        public Returns SaveNormatives(NormFinder finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    ret = cli.SaveNormatives(finder);
                }
                else
                {

                    using (var dbparam = new DbParameters())
                    {
                        ret = dbparam.SaveNormatives(finder);
                    }
                    return ret;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return ret;
        }

        public Returns InsertNewNormValues(NormFinder finder, DataSet NewNorms)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    ret = cli.InsertNewNormValues(finder, NewNorms);
                }
                else
                {

                    using (DbParameters dbparam = new DbParameters())
                    {
                        ret = dbparam.InsertNewNormValues(finder, NewNorms);
                    }
                    return ret;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return ret;
        }


        public NormParam GetTypePrmIdByNzpPrm(string nzp_prm, int normId, out Returns ret)
        //----------------------------------------------------------------------
        {
            NormParam np = new NormParam();
            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    np = cli.GetTypePrmIdByNzpPrm(nzp_prm, normId, out ret);
                }
                else
                {

                    using (DbParameters dbparam = new DbParameters())
                    {
                        np = dbparam.GetTypePrmIdByNzpPrm(nzp_prm, normId, out ret);
                    }
                    return np;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return np;
        }

        public Dictionary<TypeTarif, PrmTarifs> GetGroupedTarifsData(PrmTarifFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            var res = new Dictionary<TypeTarif, PrmTarifs>();
            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    res = cli.GetGroupedTarifsData(finder, out ret);
                }
                else
                {

                    using (DBPrmTarifs dbparam = new DBPrmTarifs())
                    {
                        res = dbparam.GetGroupedTarifsData(finder, out ret);
                    }
                    return res;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return res;
        }

        public Returns SetGroupedTarifsData(Dictionary<TypeTarif, PrmTarifs> newTarifs,
            PrmTarifFinder finder)
        //----------------------------------------------------------------------
        {
            Returns res = Utils.InitReturns();
            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    res = cli.SetGroupedTarifsData(newTarifs, finder);
                }
                else
                {

                    using (DBPrmTarifs dbparam = new DBPrmTarifs())
                    {
                        res = dbparam.SetGroupedTarifsData(newTarifs, finder);
                    }
                    return res;
                }
            }
            catch (Exception ex)
            {
                res = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return res;
        }

        public List<long> GetListLsByTarif(PrmTarifFinder finder, TypeTarif type, Tarif tarif, out Returns ret)
        {
            ret = Utils.InitReturns();
            var res = new List<long>();
            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Prm cli = new cli_Prm(WCFParams.AdresWcfHost.CurT_Server);
                    res = cli.GetListLsByTarif(finder, type, tarif, out ret);
                }
                else
                {

                    using (DBPrmTarifs dbparam = new DBPrmTarifs())
                    {
                        res = dbparam.GetListLsByTarif(finder, type, tarif, out ret);
                    }
                    return res;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }
            return res;
        }
    }
}

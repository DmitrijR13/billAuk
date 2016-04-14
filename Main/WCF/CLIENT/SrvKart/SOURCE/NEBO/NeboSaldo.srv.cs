using System;
using System.Collections.Generic;
using System.Data;
using System.ServiceModel;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Server
{

    public partial class srv_Nebo : srv_Base, I_Nebo
    {
        public Returns CreateReestrNebo(int nzp_type, string dat_s, string dat_po)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();


            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Nebo cli = new cli_Nebo(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CreateReestrNebo(nzp_type, dat_s, dat_po);
            }
            else
            {
                DbNeboSaldo db = new DbNeboSaldo();
                try
                {
                    ret = db.CreateReestrNebo(nzp_type, dat_s, dat_po);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка CreateReestrNebo() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                }
                db.Close();
            }
            return ret;
        }


        public IntfResultObjectType<List<NeboReestr>> GetReestrInfo(int nzp_nebo_reestr)
        {
            try
            {
                IntfResultObjectType<List<NeboReestr>> res =
                    new ClassDB().RunSqlAction(new IntfRequestType() { keyID = nzp_nebo_reestr }, new DbNeboSaldo().GetReestrInfo);
                res.GetReturnsType().ThrowExceptionIfError();
                return new IntfResultObjectType<List<NeboReestr>>(res.resultData);
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new IntfResultObjectType<List<NeboReestr>>(-1, ret.text);
            }
        }

        public IntfResultObjectType<List<NeboSaldo>> GetSaldoReestr(NeboSaldo neboSaldo, RequestPaging paging)
        {
            try
            {
                IntfRequestObjectType<NeboSaldo> request = new IntfRequestObjectType<NeboSaldo>(neboSaldo) { paging = paging };
                IntfResultObjectType<List<NeboSaldo>> res = new ClassDB().RunSqlAction(request, new DbNeboSaldo().GetSaldoReestr);
                res.GetReturnsType().ThrowExceptionIfError();
                return res;
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new IntfResultObjectType<List<NeboSaldo>>(-1, ret.text);
            }
        }

        public IntfResultObjectType<List<NeboSupp>> GetSuppReestr(NeboSupp neboSupp)
        {
            try
            {
                IntfRequestObjectType<NeboSupp> req = new IntfRequestObjectType<NeboSupp>(neboSupp);

                IntfResultObjectType<List<NeboSupp>> res = new ClassDB().RunSqlAction(req, new DbNeboSaldo().GetSuppReestr);
                res.GetReturnsType().ThrowExceptionIfError();

                return new IntfResultObjectType<List<NeboSupp>>(res.resultData);
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new IntfResultObjectType<List<NeboSupp>>(-1, ret.text);
            }
        }

        public IntfResultObjectType<List<NeboPaymentReestr>> GetPaymentReestr(int nzp_nebo_reestr, int nzp_area, RequestPaging paging)
        {
            try
            {
                IntfResultObjectType<List<NeboPaymentReestr>> res =
                    new ClassDB().RunSqlAction(new IntfRequestType() { keyID = nzp_nebo_reestr, parentID = nzp_area, paging = paging }, new DbNeboSaldo().GetPaymentReestr);
                res.GetReturnsType().ThrowExceptionIfError();
                return res;
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new IntfResultObjectType<List<NeboPaymentReestr>>(-1, ret.text);
            }
        }
    }

}

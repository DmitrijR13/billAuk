using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System.Collections;
using System.Data;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.REPORT;
using STCLINE.KP50.Client;
using System.IO;

namespace STCLINE.KP50.Server
{
    public class srv_Exchange : srv_Base, I_Exchange
    {
        /// <summary>
        /// Загрузка оплат от ВТБ24
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns UploadVTB24(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UploadVTB24(finder);
            }
            else
            {
                DbExchange db = new DbExchange();
                try
                {
                    ret = db.UploadVTB24(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки реестра в БЦ";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UploadVTB24" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        /// <summary>
        /// Загрузка оплат от ВТБ24
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public List<VTB24Info> GetReestrsVTB24(ExFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<VTB24Info> res = new List<VTB24Info>();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetReestrsVTB24(finder, out ret);
            }
            else
            {
                DbExchange db = new DbExchange();
                try
                {
                    res = db.GetReestrsVTB24(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки реестра в БЦ";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UploadVTB24" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
                db.Close();
            }
            return res;
        }

        public Returns DeleteReestrVTB24(ExFinder finder)
        {
            Returns ret = Utils.InitReturns();

            DbExchange db = new DbExchange();
            try
            {
                ret = db.DeleteReestrVTB24(finder);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteReestrVTB24" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
            db.Close();

            return ret;
        }


        public bool CanDistr(ExFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res;
            DbExchange db = new DbExchange();
            try
            {
                res = db.CanDistr(finder, out ret);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка CanDistr" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
            db.Close();

            return res;
        }
    }
}

using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.KP50.Gubkin.Srv
{
    public class srv_OneTime : srv_Base, I_OneTimeLoad
    {

        public Returns ReadReestrFromCbb(FilesImported finderpack, FilesImported finder, string connectionString)
        {
            var ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_OneTimeLoad cli = new cli_OneTimeLoad(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.ReadReestrFromCbb(finderpack, finder, connectionString);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                Oplats db = new Oplats();
                try
                {
                    ret = db.ReadReestrFromCbb(finderpack, finder, connectionString);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ReadReestrFromCbb" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns UploadUESCharge(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_OneTimeLoad cli = new cli_OneTimeLoad(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UploadUESCharge(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbUES db = new DbUES();
                try
                {
                    ret = db.UploadUESCharge(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки ";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UploadUESCharge" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns UploadMURCPayment(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_OneTimeLoad cli = new cli_OneTimeLoad(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UploadMURCPayment(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbMURC db = new DbMURC();

                try
                {
                    ret = db.UploadMURCPayment(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки ";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UploadMURCPayment" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

    }
}

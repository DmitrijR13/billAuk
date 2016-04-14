using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bars.KP50.CLI.Exchange.Cli;
using Bars.KP50.CLI.Exchange.Interface;
using Bars.KP50.SzExchange.UnloadForSZ;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.Srv
{
    public class Srv_UnlPassport : srv_Base, I_UnlPassport
    {
        public Returns RepeatedlyUnload(FilesImported finder)
        {
            var ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_UnlPassport cli = new cli_UnlPassport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.RepeatedlyUnload(finder);
            }
            else
            {
                RepeatedlyUnload_UnlPassport db = new RepeatedlyUnload_UnlPassport();
                try
                {
                    ret = db.RepeatedlyUnload(finder.bank, finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ReadReestrFromCbb" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

    }
}


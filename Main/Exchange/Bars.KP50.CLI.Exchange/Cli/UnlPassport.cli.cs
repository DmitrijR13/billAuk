using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Bars.KP50.CLI.Exchange.Interface;
using STCLINE.KP50.Client;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.CLI.Exchange.Cli
{
    public partial class cli_UnlPassport : cli_Base, I_UnlPassport
    {
        //I_Exchange remoteObject;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nzp_server">Код удаленного сервера</param>
        /// <param name="nzp_role">Код подсистемы</param>
        public cli_UnlPassport(int nzp_server)
            : base()
        {
            //_cli_Exchange(nzp_server, Constants.roleDebt);
        }

        IUnlPassportRemoteObject getRemoteObject()
        {
            return getRemoteObject<IUnlPassportRemoteObject>(WCFParams.AdresWcfWeb.srvUnlPassport);
        }

        public Returns RepeatedlyUnload(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.RepeatedlyUnload(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка RepeatedlyUnload\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка RepeatedlyUnload\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
    }
}

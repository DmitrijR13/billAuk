using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using System.Diagnostics;

namespace STCLINE.KP50.Server
{
    public class srv_Archive : srv_Base, I_Archive
    {
        /// <summary>
        /// Делает архив БД
        /// </summary>
        /// <param name="finder">Указывается ID пользователя </param>
        /// <returns>Результат операции</returns>
        public Returns MakeArchive(Finder finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                cli_Archive cli = new cli_Archive();
                ret = cli.MakeArchive(finder);
            }
            else
            {
                SrvRun.TaskStop();
                HostBase.CloseAllHost(true);

                // Архивация. Начало
                DbArchive db = new DbArchive();
                db.MakeArchive();
                // Конец

                SrvRun.TcpHostingStart(WCFParams.AdresWcfHost.Adres);
                SrvRun.TaskStarting();
            }

            return ret;
        }
    }
}

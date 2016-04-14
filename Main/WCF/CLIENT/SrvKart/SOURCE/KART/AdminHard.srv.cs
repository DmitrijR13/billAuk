using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;

using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;

namespace STCLINE.KP50.Server
{
    //----------------------------------------------------------------------
    public class srv_AdminHard : srv_Base, I_AdminHard
    {
      




        /// <summary>
        /// Подготовить данные для печати ЛС
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public Returns PreparePrintInvoices(List<PointForPrepare> finder)
        {
            Returns ret = new Returns();
            if (SrvRunProgramRole.IsBroker)
            {
                cli_AdminHard cli = new cli_AdminHard(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.PreparePrintInvoices(finder);
            }
            else
            {
                try
                {
                    DbAdminHard db = new DbAdminHard();
                    ret = db.PreparePrintInvoices(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка PreparePrintInvoices\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return ret;
        }


        public Returns UploadInDb(FilesImported finder, UploadOperations operation, UploadMode mode)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdminHard cli = new cli_AdminHard(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UploadInDb(finder, operation, mode);
            }
            else
            {
                DbAdminHard db = new DbAdminHard();
                try
                {
                    switch (operation)
                    {
                        case UploadOperations.Area:
                            if (mode == UploadMode.Add) ret = db.UploadAreaInDb(finder, true);
                            else ret = db.UploadAreaInDb(finder);
                            break;
                        case UploadOperations.Dom:
                            if (mode == UploadMode.Add) ret = db.UploadDomInDb(finder, true);
                            else ret = db.UploadDomInDb(finder);
                            break;
                        case UploadOperations.Kvar:
                            if (mode == UploadMode.Add) ret = db.UploadLsInDb(finder, true);
                            else ret = db.UploadLsInDb(finder);
                            break;
                        case UploadOperations.Supp:
                            if (mode == UploadMode.Add) ret = db.UploadSuppInDb(finder, true);
                            else ret = db.UploadSuppInDb(finder);
                            break;
                        default: ret = new Returns(false, "Неверное наименование операции"); break;
                    }

                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции UploadInDb";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UploadInDb\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }


     
     

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System.Collections;
using System.Data;
using STCLINE.KP50.Interfaces;

using STCLINE.KP50.Client;
using System.IO;

namespace STCLINE.KP50.Server
{
    public partial class srv_Exchange : srv_Base, I_Exchange
    {

        #region ВТБ24
        /// <summary>
        /// Загрузка оплат от ВТБ24
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns CheckVtb24(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CheckVtb24(finder);
            }
            else
            {
                DbExchange db = new DbExchange();
                try
                {
                    ret = db.CheckVtb24(finder);
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
            if (SrvRunProgramRole.IsBroker)
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

        #endregion

        #region Обмен с поставщиками




        /// <summary>
        /// Загрузка списка файлов синхронизации
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public List<IReestrExSuppSyncLs> GetReestrSyncLs(ExFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<IReestrExSuppSyncLs> res = new List<IReestrExSuppSyncLs>();

            DbExchange db = new DbExchange();
            try
            {
                res = db.GetReestrSyncLs(finder, out ret);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции загрузки списка файлов синхронизации";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetReestrSyncLs" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
            db.Close();

            return res;
        }

        public Returns DeleteReestrRow(ExFinder finder)
        {
            Returns ret = Utils.InitReturns();

            DbExchange db = new DbExchange();
            try
            {
                ret = db.DeleteReestrRow(finder);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteReestrRow" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
            db.Close();

            return ret;
        }



        /// <summary>
        /// Загрузка списка файлов синхронизации
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public List<IReestrExSuppChangeLs> GetReestrChangeLs(ExFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<IReestrExSuppChangeLs> res = new List<IReestrExSuppChangeLs>();

            DbExchange db = new DbExchange();
            try
            {
                res = db.GetReestrChangeLs(finder, out ret);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции загрузки списка файлов синхронизации";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetReestrChangeLs" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
            db.Close();

            return res;
        }


        #endregion

        #region Обмен с соцзащитой

        public Returns DeleteFromExchangeSZ(Finder finder, int nzp_ex_sz)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();


            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteFromExchangeSZ(finder, nzp_ex_sz);
            }
            else
            {
                DbExchange db = new DbExchange();
                try
                {
                    ret = db.DeleteFromExchangeSZ(finder, nzp_ex_sz);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteFromExchangeSZ() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                }
                db.Close();
            }
            return ret;
        }

        #endregion


        /// <summary>
        /// Загрузка расходов по счетчикам
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<SimpleLoadClass> GetSimpleLoadData(FilesImported finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SimpleLoadClass> res = new List<SimpleLoadClass>();

            DbExchange db = new DbExchange();
            try
            {
                res = db.GetSimpleLoadData(finder, out ret);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции загрузки списка файлов синхронизации";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetReestrSyncLs" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
            db.Close();

            return res;
        }


        public Returns Delete(SimpleLoadClass finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.Delete(finder);
            }
            else
            {
                DbExchange db = new DbExchange();
                try
                {
                    ret = db.Delete(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка Delete() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                }
                db.Close();
            }
            return ret;
        }

        public Returns CheckSimpleLoadFileExixsts(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CheckSimpleLoadFileExixsts(finder);
            }
            else
            {
                DbExchange db = new DbExchange();
                try
                {
                    ret = db.CheckSimpleLoadFileExixsts(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка Delete() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                }
                db.Close();
            }
            return ret;
        }

        public Returns MoveLoadToArchive(SimpleLoadClass finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.MoveLoadToArchive(finder);
            }
            else
            {
                try
                {
                    using (DbExchange db = new DbExchange())
                    {
                        ret = db.MoveLoadToArchive(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка MoveLoadToArchive() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                }
            }
            return ret;
        }

        public Returns MoveLoadedSourcePackToArchive(SimpleLoadClass finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.MoveLoadedSourcePackToArchive(finder);
            }
            else
            {
              
                try
                {
                    using (DbExchange db = new DbExchange())
                    {
                         ret = db.MoveLoadedSourcePackToArchive(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка MoveLoadToArchive() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                }
            }
            return ret;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.Load;
using Bars.KP50.Load.Obninsk;
using Bars.KP50.Load.Obninsk.CountersUnload;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Server
{
    public partial class srv_Exchange
    {

        #region Загрузка счетчиков

        public Returns LoadSimpleCounters(int nzpUser, string webLogin, string fileName, string userFileName)
            //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();


            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LoadSimpleCounters(nzpUser, webLogin, fileName, userFileName);
            }
            else
            {

                var db = new LoadObnCounters();
                var finder = new FilesImported
                {
                    nzp_user = nzpUser,
                    webLogin = webLogin,
                    saved_name = fileName,
                    ex_path = userFileName
                };
                var thread = new System.Threading.Thread(db.StartWithObject);
                thread.IsBackground = true;
                thread.Start(finder);
            }
            return ret;
        }

        #endregion

        #region Загрузка начислений

        public Returns LoadSuppCharge(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LoadSuppCharge(finder);
            }
            else
            {
                var db = new LoadSuppCharge();
                var thread = new System.Threading.Thread(db.StartWithObject);
                thread.IsBackground = true;
                thread.Start(finder);
            }
            return ret;
        }

        /// <summary>
        /// Получить список услуг из промежуточной таблицы начислений сторонних поставщиков
        /// </summary>
        /// <param name="nzpLoad">Код загрузки</param>
        /// <returns></returns>
        public List<string> GetServiceNameLoadSuppCharge(int nzpLoad)
        {
            List<string> ret;


            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetServiceNameLoadSuppCharge(nzpLoad);
            }
            else
            {
                var db = new SuppChargeService();
                ret = db.GetServiceNameLoadSuppCharge(nzpLoad);

            }
            return ret;
        }


        /// <summary>
        /// Удаляет начисления сторонних поставщиков
        /// </summary>
        /// <param name="nzpLoad">Код загрузки</param>
        /// <returns></returns>
        public Returns DeleteSuppCharge(int nzpLoad)
        {
            Returns ret;


            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteSuppCharge(nzpLoad);
            }
            else
            {
                var db = new SuppChargeService();
                ret = db.DeleteSuppCharge(nzpLoad);

            }
            return ret;
        }



        /// <summary>
        /// Разбор начисления сторонних поставщиков
        /// </summary>
        /// <param name="nzpLoad">Код загрузки</param>
        /// <param name="services">Список услуг</param>
        /// <returns></returns>
        public Returns DisassemleLoadSuppCharge(int nzpLoad, Dictionary<string, int> services)
            //----------------------------------------------------------------------
        {
            Returns ret;


            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DisassemleLoadSuppCharge(nzpLoad, services);
            }
            else
            {
                var db = new SuppChargeService();
                ret = db.DisassemleLoadSuppCharge(nzpLoad, services);

            }
            return ret;
        }

        #endregion

        # region Загрузка оплат

        public Returns LoadOplFromKass(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LoadOplFromKass(finder);
            }
            else
            {
                var db = new LoadOplatyKassa();
                var thread = new System.Threading.Thread(db.StartWithObject);
                thread.IsBackground = true;
                thread.Start(finder);
            }
            return ret;
        }

        #endregion

        public Returns LoadPayments(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LoadPayments(finder);
            }
            else
            {
                var db = new LoadPayments();
                var thread = new System.Threading.Thread(db.StartWithObject);
                thread.IsBackground = true;
                thread.Start(finder);
            }
            return ret;
        }

        public Returns ImportParams(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.ImportParams(finder);
            }
            else
            {
                var db = new ImportParams();
                var thread = new System.Threading.Thread(db.StartWithObject);
                thread.IsBackground = true;
                thread.Start(finder);
            }
            return ret;
        }

        public List<PrmTypes> GetParamSprav(ParamCommon finder, out Returns ret)
        {
            List<PrmTypes> list;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetParamSprav(finder, out ret);
            }
            else
            {
                using (var db = new DbExchange())
                {
                    list = db.GetParamSprav(finder, out ret);
                }

            }
            return list;
        }

        public Returns LoadCountersReadings(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LoadCountersReadings(finder);
            }
            else
            {
                CountersRSOByNzpCounter cnt = new CountersRSOByNzpCounter(
                    new MustCalcCounters(), 
                    new DiffValueCounters(), 
                    new SetProgress());
                var db = new CountersLoadMain(cnt);
                var thread = new System.Threading.Thread(db.StartWithObject);
                thread.IsBackground = true;
                thread.Start(finder);
            }
            return ret;
        }

        public Returns UnoadCountersReadings(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Exchange cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LoadCountersReadings(finder);
            }
            else
            {
                UnloadLastValueCounters cnt = new UnloadLastValueCounters(new SetProgress());
                var db = new UnloadCountersMain(cnt);
                var thread = new System.Threading.Thread(db.StartWithObject);
                thread.IsBackground = true;
                thread.Start(finder);
            }
            return ret;
        }
    }
}

using System;
using System.Collections.Generic;
using Bars.KP50.Load.Obninsk;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;
using Bars.KP50.DB.Exchange.Unload;

namespace STCLINE.KP50.Server
{
    public partial class srv_Exchange 
    {

        #region Загрузка счетчиков

        /// <summary>
        /// Выгрузка в ЦХД
        /// </summary>
        /// <param name="nzpUser">Код пользователя</param>
        /// <param name="year">Год выгрузки</param>
        /// <param name="month">Месяц</param>
        /// <param name="pref">Префикс схемы, если схема есть</param>
        /// <param name="listNzpArea">Список УК, если есть</param>
        /// <returns></returns>
        public Returns UnloadToDw(int nzpUser, int year, int month, string pref, List<int> listNzpArea)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();


            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                var cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnloadToDw(nzpUser, year, month, pref, listNzpArea);
            }
            else
            {
                var db = new Sections(year, month, listNzpArea);



                var finder = new FilesImported
                {
                    nzp_user = nzpUser,
                    month = month.ToString(),
                    year = year.ToString(),
                    pref = pref,
                    ListNzpArea = listNzpArea
                };
                

                var thread = new System.Threading.Thread(db.StartWithObject);
                thread.IsBackground = true;
                thread.Start(finder);
            }
            return ret;
        }


      
      
        #endregion

       


    }
}

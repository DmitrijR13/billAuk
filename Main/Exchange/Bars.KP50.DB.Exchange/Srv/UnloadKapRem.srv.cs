using System.ComponentModel;
using System.Threading;
using Bars.KP50.DB.Exchange.UnloadKapRem;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;

namespace STCLINE.KP50.Server
{
    public partial class srv_Exchange
    {
         #region Выгрузка в Капремонт

        /// <summary>
        /// Выгрузка в Капремонт
        /// </summary>
        /// <param name="nzpUser">Код пользователя</param>
        /// <param name="year">Год выгрузки</param>
        /// <param name="month">Месяц выгрузки</param>
        /// <param name="pref">Префикс схемы если есть</param>
        /// <returns></returns>
        public Returns UnloadKapRem(int nzpUser, string year, string month, string pref)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                var cli = new cli_Exchange(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnloadKapRem(nzpUser, year, month, pref);
            }
            else
            {
                var db = new UnloadKapRemont();



                var finder = new FilesImported
                {
                    nzp_user = nzpUser,
                    month = month,
                    year = year,
                    pref = pref
                };
                

                var thread = new Thread(db.StartWithObject);
                thread.IsBackground = true;
                thread.Start(finder);
            }
            return ret;
        }
#endregion
    }
}

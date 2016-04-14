namespace STCLINE.KP50.Client
{
    using System;
    using System.ServiceModel;
    using System.Collections.Generic;
    using Interfaces;
    using Global;


    public partial class cli_Exchange
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
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnloadKapRem(nzpUser, year, month, pref);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }
        #endregion 
    
    }
}

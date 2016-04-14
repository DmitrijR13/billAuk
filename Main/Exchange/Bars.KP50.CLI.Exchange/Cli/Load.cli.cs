namespace STCLINE.KP50.Client
{
    using System;
    using System.ServiceModel;
    using System.Collections.Generic;
    using Interfaces;
    using Global;


    public partial class cli_Exchange 
    {

 
        #region Загрузка счетчиков 

        public Returns LoadSimpleCounters(int nzpUser, string webLogin, string fileName, string userFileName)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LoadSimpleCounters(nzpUser, webLogin, fileName, userFileName);
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


        #region Загрузка начислений для сторонних поставщиков
        /// <summary>
        /// Загрузка начислений сторонних поставщиков
        /// </summary>
        /// <param name="nzpUser">Код пользователя</param>
        /// <param name="fileName">Имя файла во временном хранилище</param>
        /// <param name="userFileName">Имя файла, указанного пользователем</param>
        /// <param name="pref">Префикс схемы, если выбран</param>
        /// <param name="nzpSupp">Код договора (поставщика)</param>
        /// <param name="date">Месяц, в который загружаются данные</param>
        /// <returns></returns>
        public Returns LoadSuppCharge(FilesImported finder)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LoadSuppCharge(finder);
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


        /// <summary>
        /// Получение списка услуг в загружаемом файле сторонних поставщиков
        /// </summary>
        /// <param name="nzpLoad">Код загрузки</param>
        /// <returns></returns>
        public List<string> GetServiceNameLoadSuppCharge(int nzpLoad)
        {
            var ret = new List<string>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetServiceNameLoadSuppCharge(nzpLoad);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }


        /// <summary>
        /// Разборка начислений сторонних поставщиков
        /// </summary>
        /// <param name="nzpLoad"></param>
        /// <param name="services"></param>
        /// <returns></returns>
        public Returns DisassemleLoadSuppCharge(int nzpLoad, Dictionary<string, int> services)
        {
            var ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DisassemleLoadSuppCharge(nzpLoad, services);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        
            
        }


        /// <summary>
        /// Удаляет начисления сторонних поставщиков
        /// </summary>
        /// <param name="nzpLoad"></param>
        /// <returns></returns>
        public Returns DeleteSuppCharge(int nzpLoad)
        {
            var ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteSuppCharge(nzpLoad);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;


        }

        public Returns LoadOplFromKass(FilesImported finder)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LoadOplFromKass(finder);
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
        public Returns LoadPayments(FilesImported finder)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LoadPayments(finder);
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

        public Returns ImportParams(FilesImported finder)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.ImportParams(finder);
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

        public List<PrmTypes> GetParamSprav(ParamCommon finder, out Returns ret)
        {
            List<PrmTypes> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetParamSprav(finder, out ret);
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

            return list;
        }

        public Returns LoadCountersReadings(FilesImported finder)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LoadCountersReadings(finder);
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

        public Returns UnoadCountersReadings(FilesImported finder)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UnoadCountersReadings(finder);
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

        public Returns MoveLoadToArchive(SimpleLoadClass finder)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.MoveLoadToArchive(finder);
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

        public Returns MoveLoadedSourcePackToArchive(SimpleLoadClass finder)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.MoveLoadedSourcePackToArchive(finder);
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
    }
}

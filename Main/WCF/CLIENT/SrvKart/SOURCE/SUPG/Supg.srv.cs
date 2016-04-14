using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System.Collections;
using System.Data;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.REPORT;
using STCLINE.KP50.Client;
using System.IO;

namespace STCLINE.KP50.Server
{

    public class srv_supg : srv_Base, I_Supg
    {
        /// <summary>
        /// Сервис работы с недопоставками
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool NedopService(int proc, JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            try
            {

                switch (proc)
                {
                    case 1:
                        {
                            return db.SaveMakeNedop(finder, out ret);                            
                        }

                    default:
                        {
                            return false;                           
                        }
                }                
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }





        /// <summary>
        /// Получает классификацию сообщения
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <param name="ret">результат</param>
        /// <returns>Список классиффикаций сообщения</returns>
        public Dictionary<int, string> GetClassMessage(OrderContainer finder, out Returns ret)
        {
            Supg db = new Supg();
            Dictionary<int, string> dict = null;
            ret = Utils.InitReturns();
            try
            {
                dict = db.TematicaLib(finder, out ret);
                return dict;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message,MonitorLog.typelog.Error,true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        /// <summary>
        /// Добавления заказа
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <returns>номер сообщения</returns>
        public int AddOrder(OrderContainer finder)
        {
            Returns ret = Utils.InitReturns();
            Supg db = new Supg();
            try
            {
                return db.Add_Order(finder, out ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                return -1;
            }
            finally
            {
                db.Close();
            }
        }
        
        /// <summary>
        /// Получает список заявлений(жалоб)
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <param name="ret">Результат выполнения</param>
        /// <returns>Список заявлений</returns>
        public List<OrderContainer> Find_Orders(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            try
            {
                return db.Find_Orders(finder, out ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        /// <summary>
        /// Получает библиотеку со списками для заполнения полей поиска
        /// </summary>
        /// <returns>Библиотека со списками для заполнения полей поиска </returns>
        public Dictionary<string, Dictionary<int, string>> GetSupgLists(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            try
            {
                return db.GetSupgLists(finder, out ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }


        /// <summary>
        /// Получает библиотеку со списком для заполнения полей поиска
        /// </summary>
        /// <returns>Библиотека со списками для заполнения полей поиска </returns>
        public List<Dest> GetDestName(int nzp_serv, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            List<Dest> retList = null;
            try
            {
                switch (oper)
                {
                    case enSrvOper.GetDestName:
                        {
                            retList = db.GetDestName(nzp_serv, out ret);
                            break;
                        }

                    case enSrvOper.GetClaimCatalog:
                        {
                            retList = db.GetClaimsCatalog(out ret);
                            break;
                        }
                }

                return retList;
                
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public int FindZvk (SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            int res = -1;
            Supg db = new Supg();

            try
            {
                res = db.FindZvk(finder, out ret);
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формировании результата поиска FindZvk: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return res;
            }
            finally
            {
                db.Close();
            }
        }


        public ZvkFinder FastFindZk(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            ZvkFinder res = new ZvkFinder();
            Supg db = new Supg();

            try
            {
                res = db.FastFindZk(finder, out ret);
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формировании результата поиска FastFindZk: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return null;
            }
            finally
            {
                db.Close();
            }
        }


        public DataSet GetZakazReport(SupgFinder finder, string table_name, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataSet res = new DataSet();
            Supg db = new Supg();

            try
            {
                res = db.GetZakazReport(finder, table_name, out ret);
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формировании результата поиска GetZakazReport: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return null;
            }
            finally
            {
                db.Close();
            }
        }


        public ZvkFinder GetCarousel(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            ZvkFinder res = new ZvkFinder();
            Supg db = new Supg();

            try
            {
                res = db.GetCarousel(finder, out ret);
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формировании результата поиска GetCarousel: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return null;
            }
            finally
            {
                db.Close();
            }
        }


        public List<ServiceForwarding> GetServices(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            Supg db = new Supg();
            List<ServiceForwarding> listServices = null;

            try
            {
                listServices = db.GetServices(finder, out ret);
                return listServices;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения списка услуг: " + ex.Message,MonitorLog.typelog.Error,true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public List<ZvkFinder> GetFindZvk(SupgFinder finder, int flag, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            List<ZvkFinder> ZvkList = null;
            try
            {
                ZvkList = db.GetFindZvk(finder, flag, out ret);
                return ZvkList;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при получении списка заявок при поиске: " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public OrderContainer Result_Generating_Procedure(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            Supg db = new Supg();
            OrderContainer Container = null;

            try
            {
                Container = db.Result_Generating_Procedure(finder, out ret);
                return Container;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения результата, срока выполнения, факта выполнения заявки: " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }


        public bool AddReaddress(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            Supg db = new Supg();
            bool res = false;

            try
            {
                res = db.AddReaddress(finder, out ret);
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления переадресации :" + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        public List<ServiceForwarding> GetReadress(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ServiceForwarding> listReadress = null;
            Supg db = new Supg();
            try
            {
                listReadress = db.GetReadress(finder, out ret);
                return listReadress;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения списка переадресаций : " + ex.Message,MonitorLog.typelog.Error,true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public ServiceForwarding GetServiceForward_One(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            ServiceForwarding service = null;
            Supg db = new Supg();
            try
            {
                service = db.GetServiceForward_One(finder, out ret);
                return service;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения информации о переадресации : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public bool SaveCommentsReadress(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            Supg db = new Supg();
            try
            {
                res = db.SaveCommentsReadress(finder, out ret);
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка обновления информации о переадресации : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        public OrderContainer Find_Orders_One(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            OrderContainer order = null;
            Supg db = new Supg();
            try
            {
                order = db.Find_Orders_One(finder, out ret);
                return order;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка обновления информации о переадресации : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public bool UpdateZvk(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            Supg db = new Supg();
            try
            {
                res = db.UpdateZvk(finder, out ret);
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка обновления информации о заявке : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        public Dictionary<int, string> GetAllServices(int nzp_user, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = null;
            Supg db = new Supg();
            try
            {
                retDic = db.GetAllServices(nzp_user,out ret);
                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения информации об Услугах : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public Dictionary<int, string> GetDest(int nzp_serv, int nzp_user, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = null;
            Supg db = new Supg();
            try
            {
                retDic = db.GetDest(nzp_serv, nzp_user, out ret);
                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения информации об Услугах : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public Dest GetNedops(Dest finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dest retDest = null;
            Supg db = new Supg();
            try
            {
                retDest = db.GetNedops(finder, out ret);
                return retDest;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения информации о недопоставке : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        //public string GetSupplier(int nzp_kvar, int nzp_user, int nzp_serv, string act_date, out Returns ret)
        public string GetSupplier(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            string retStr = "";
            Supg db = new Supg();
            try
            {
                retStr = db.GetSupplier(finder,out ret);
                return retStr;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения информации о поставщике : " + ex.Message, MonitorLog.typelog.Error, true);
                return "";
            }
            finally
            {
                db.Close();
            }
        }

        public Dictionary<int, string> GetSuppliersAll(int nzp_user, int supp_filter , string pref, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = null;
            Supg db = new Supg();
            try
            {
                retDic = db.GetSuppliersAll(nzp_user, supp_filter ,pref ,out ret);
                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения списка поставщиков : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public bool AddJobOrder(ref JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            Supg db = new Supg();
            try
            {
                res = db.AddJobOrder(ref finder, out ret);
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления наряд-заказа : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        public List<JobOrder> GetJobOrders(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<JobOrder> retList = null;
            Supg db = new Supg();
            try
            {
                retList = db.GetJobOrders(finder, out ret);
                return retList;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения наряд-заказов : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public JobOrder GetJobOrderForm(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            JobOrder jo;
            Supg db = new Supg();
            try
            {
                jo = db.GetJobOrderForm(finder, out ret);
                return jo;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения выбранного наряд-заказа : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public Dictionary<int, string> GetJobOrderResultsAll(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = null;
            Supg db = new Supg();
            try
            {
                retDic = db.GetJobOrderResultsAll(finder, out ret);
                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public decimal GetDolgLs(Ls finder, int dat_y, int dat_m, out Returns ret)
        {
            ret = Utils.InitReturns();
            decimal resDolg = -1;
            Supg db = new Supg();
            try
            {
                resDolg = db.GetDolgLs(finder, dat_y, dat_m, out ret);
                return resDolg;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return -1;
            }
            finally
            {
                db.Close();
            }
        }

        public Dictionary<int, string> GetAttistation(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = null;
            Supg db = new Supg();
            try
            {
                retDic = db.GetAttistation(finder, out ret);
                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        //public bool UpdateZakaz(JobOrder finder, out Returns ret)
        //{
        //    ret = Utils.InitReturns();
        //    bool res = false;
        //    Supg db = new Supg();
        //    try
        //    {
        //        res = db.UpdateZakaz(finder, out ret);
        //        return res;
        //    }
        //    catch (Exception ex)
        //    {
        //        MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
        //        return false;
        //    }
        //    finally
        //    {
        //        db.Close();
        //    }
        //}

        public bool AddRepeatedJobOrder(ref JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            Supg db = new Supg();
            try
            {
                res = db.AddRepeatedJobOrder(ref finder, out ret);
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        public bool IsOrderClose(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            Supg db = new Supg();
            try
            {
                res = db.IsOrderClose(finder, out ret);
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        public List<JobOrder> GetNedopsAll(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<JobOrder> retList = null;
            Supg db = new Supg();
            try
            {
                retList = db.GetNedopsAll(finder, out ret);
                return retList;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public bool CopyFields_WhenResultChanged(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            Supg db = new Supg();
            try
            {
                res = db.CopyFields_WhenResultChanged(finder, out ret);
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        public bool AddNedopJobOrder(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            Supg db = new Supg();
            try
            {
                res = db.AddNedopJobOrder(finder, out ret);
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        public bool UpdateZvk_armOperator(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            Supg db = new Supg();
            try
            {
                res = db.UpdateZvk_armOperator(finder, out ret);
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        public Returns DbChangeMarksSpisSupg(SupgFinder finder, List<SupgFinder> list0, List<SupgFinder> list1)
        {
            Returns ret = Utils.InitReturns();
            Supg db = new Supg();
            try
            {
                ret = db.ChangeMarksSpisSupg(finder, list0, list1);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка DbChangeMarksSpisSupg: " + ex.Message, MonitorLog.typelog.Error, true);
                return ret;
            }
            finally
            {
                db.Close();
            }
        }

        //статистика
        public Returns GetSupgStatistics(SupgFinder finder)
        {
            Returns ret = Utils.InitReturns();
            Supg db = new Supg();
            try
            {
                ret = db.GetSupgStatistics(finder);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка GetSupgStatistics: " + ex.Message, MonitorLog.typelog.Error, true);
                return ret;
            }
            finally
            {
                db.Close();
            }
        }


        public bool UpdateStatusJobOrder(JobOrder finder, int status, out Returns ret)
        {            
            bool res = false;
            Supg db = new Supg();
            try
            {
                res = db.UpdateStatusJobOrder(finder, status, out ret);
                return res;
            }
            catch (Exception ex)
            {
                ret = Utils.InitReturns();
                ret.result = false;
                MonitorLog.WriteLog("Ошибка UpdateStatusJobOrder: " + ex.Message, MonitorLog.typelog.Error, true);
                return res;
            }
            finally
            {
                db.Close();
            }
        }

        public bool UpdateJobOrder(JobOrder finder,enSupgProc proc ,out Returns ret)
        {
            bool res = false;
            Supg db = new Supg();
            ret = Utils.InitReturns();
            try
            {
                switch (proc)
                {
                    //обновить основные данные наряда-заказа
                    case enSupgProc.UpdateDataJO:
                        {
                            res = db.UpdateJobOrder(finder, out ret);
                            break;
                        }

                    //обновить результат наряда-заказа
                    case enSupgProc.UpdateResultJO:
                        {
                            res = db.UpdateZakaz(finder, out ret);
                            break;
                        }

                    //обновить документ контрольных сроков выполнения н-з
                    case enSupgProc.UpdateDocPeriod:
                        {
                            res = db.UpdateDocumentPeriod(finder, out ret);
                            break;
                        }
                }

                return res;
            }
            catch (Exception ex)
            {
                ret = Utils.InitReturns();
                ret.result = false;
                MonitorLog.WriteLog("Ошибка UpdateJobOrder\n " + proc + ": " + ex.Message, MonitorLog.typelog.Error, true);
                return res;
            }
            finally
            {
                db.Close();
            }
        }


        public List<Journal> GetJournal(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            List<Journal> Journal = null;
            try
            {
                Journal = db.GetJournal(finder, out ret);
                return Journal;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при получении журнала выгрузки: " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }


        public bool NedopForming(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            try
            {
                return db.NedopForming(finder, out ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формировании недопоставок: " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        public bool NedopPlacement(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            try
            {
                return db.NedopPlacement(finder, out ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при распределении недопоставок: " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        public bool NedopUnload(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            try
            {
                return db.NedopUnload(finder, out ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формировании файла выгрузки недопоставок: " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }
        
        public List<JobOrder> GetSpisNedop(JobOrder job_ord, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            List<JobOrder> reslist = new List<JobOrder>();
            try
            {
                reslist =  db.GetSpisNedop(job_ord, out ret);
                return reslist;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формировании списка недопоставок по наряду-заказу: " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }



        public int SetZakazActActual(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            try
            {
                return db.SetZakazActActual(finder, out ret);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при выставлении признака формирования недопоставки: " + ex.Message, MonitorLog.typelog.Error, true);
                return -1;
            }
            finally
            {
                db.Close();
            }
        }

        public bool DeleteFromJournal(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            try
            {
                return db.DeleteFromJournal(finder, out ret);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при удалении записи из журнала: " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        public bool UpdateJournal(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            try
            {
                return db.UpdateJournal(finder, out ret);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при изменении статуса записи в журнале: " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }
        
        public string DbMakeWhereString(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Supg db = new Supg();
            try
            {
                return db.MakeWhereStringGroup(finder, out ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при удалении формировании запроса при поиске лицевых счетов из шаблона поиска по заявкам: " + ex.Message, MonitorLog.typelog.Error, true);
                return "";
            }
            finally
            {
                db.Close();
            }
        }

        /// <summary>
        /// Процедура получения справочника "Дополнительные отметки"
        /// </summary>
        public Dictionary<int, string> GetAnswers(out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = null;
            Supg db = new Supg();
            try
            {
                retDic = db.GetAnswers(out ret);
                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public List<SupgAct> GetActs(SupgActFinder finder, enSrvOper oper, out Returns ret)
        {
            List<SupgAct> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Supg cli = new cli_Supg(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetActs(finder, oper, out ret);
            }
            else
            {
                Supg db = new Supg();
                try
                {
                    switch (oper)
                    {
                        case enSrvOper.SrvFind:
                            db.FindActs(finder, out ret);
                            if (ret.result)
                                list = db.GetActs(finder, out ret);
                            else
                                list = null;
                            break;
                        case enSrvOper.SrvGet:
                            list = db.GetActs(finder, out ret);
                            break;

                        case enSrvOper.GetPlannedWorks:
                            list = db.GetPlannedWorks(finder, out ret);
                            break;

                        case enSrvOper.GetWorksType:
                            list = db.GetWorksTypes(out ret);
                            break;                    
    
                        case enSrvOper.GetPlannedWork:
                            list = db.GetPlannedWork(finder, out ret);
                            break;
                        case enSrvOper.GetPlannedWorkKvar:
                            list = db.GetKvarActs(finder, out ret);
                            break;

                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции получения списка плановых работ");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetActs(" + oper.ToString() + ") \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return list;
        }

        public bool CheckToClose(JobOrder finder, string ord, out Returns ret)
        {
            ret = Utils.InitReturns();            
            Supg db = new Supg();
            try
            {
                 return db.CheckToClose(finder,ord,out ret);                
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        public bool UpdatePlannedWorks(ref SupgAct finder, enSrvOper oper, out Returns ret)
        {
            bool res = false;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Supg cli = new cli_Supg(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.UpdatePlannedWorks(ref finder, oper, out ret);
            }
            else
            {
                Supg db = new Supg();
                try
                {
                    switch (oper)
                    {                       
                        case enSrvOper.AddPlannedWork:
                            res = db.AddPlannedWork(ref finder,out ret);
                            break;

                        case enSrvOper.UpdatePlannedWork:
                            res = db.UpdatePlannedWork(ref finder, out ret);
                            break;

                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции обновления плановых работ");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetActs(" + oper.ToString() + ") \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return res;
        }

        
        public List<ServiceForwarding> GetServiceCatalog(out Returns ret)
         {
            ret = Utils.InitReturns();

            Supg db = new Supg();
            List<ServiceForwarding> listServices = null;
            
            try
            {
                listServices = db.GetServices(null, out ret);
                return listServices;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения списка служб и организаций: " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public bool UpdateServiceCatalog(ServiceForwarding finder, out Returns ret)
        {
            bool res = false;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Supg cli = new cli_Supg(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.UpdateServiceCatalog(finder, out ret);
            }
            else
            {
                Supg db = new Supg();
                try
                {
                    res = db.UpdateServiceCatalog(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции обновления справочника служб");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UpdateServiceCatalog() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return res;
        }

        public Dictionary<int, string> GetPhoneList(string pref, int nzp_kvar, out Returns ret)
        {
            Dictionary<int,string> Phones =null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Supg cli = new cli_Supg(WCFParams.AdresWcfHost.CurT_Server);
                Phones = cli.GetPhoneList(pref, nzp_kvar, out ret);
            }
            else
            {
                Supg db = new Supg();
                try
                {
                    Phones = db.GetPhoneList(pref, nzp_kvar, out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции выборки списка телефонов");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetPhoneList() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return Phones;
        }

        public bool GetSuppEMail(string nzp_supp, out Returns ret)
        {
            bool res = false;
            
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Supg cli = new cli_Supg(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetSuppEMail(nzp_supp, out ret);
            }
            else
            {
                Supg db = new Supg();
                try
                {
                    res = db.GetSuppEMail(nzp_supp, out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции выборки электронного адреса");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSuppEMail() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return res;
        }


        /// <summary>
        /// Обновление справочников
        /// </summary>
        /// <param name="finder">объект поиска</param>
        /// <param name="oper">операция</param>        
        /// <returns>результат</returns>
        public bool UpdateSpravSupg (Dest finder, enSrvOper oper, out Returns ret)
        {
            bool res = false;
            ret = Utils.InitReturns();
            string funcName = "";

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Supg cli = new cli_Supg(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.UpdateSpravSupg(finder, oper , out ret);                
            }
            else
            {
                Supg db = new Supg();
                try
                {
                    switch (oper)
                    {
                        case enSrvOper.sprav_updateClaims:
                            {
                                funcName = "Обновление справочника претензий";
                                res = db.UpdateClaimsCatalog(finder, out ret);
                                break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции: " + funcName);
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV:" + funcName  + "() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return res;
        }

        /// <summary>
        /// Заполнение таблицы БД СУПГ ls_saldo данными о долгах
        /// </summary>
        /// <returns>результат</returns>
        public bool FillLSSaldo(out Returns ret)
        {
            bool res = false;
            ret = Utils.InitReturns();
            
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Supg cli = new cli_Supg(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.FillLSSaldo(out ret);
            }
            else
            {
                Supg db = new Supg();
                try
                {
                    res = db.FillLSSaldo(out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции FillLSSaldo");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV:"  + "() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return res;
        }

        /// <summary>
        /// Заполнение таблицы БД СУПГ ls_tarif данными о поставщиках услуг
        /// </summary>
        /// <returns>результат</returns>
        public bool FillLSTarif(out Returns ret)
        {
            bool res = false;
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Supg cli = new cli_Supg(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.FillLSTarif(out ret);
            }
            else
            {
                Supg db = new Supg();
                try
                {
                    res = db.FillLSTarif(out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции FillLSTarif");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV:" + "() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return res;
        }

        /// <summary>
        /// Получить справочник Классификация сообщений
        /// </summary>
        /// <returns>результат</returns>
        public List<Sprav> GetThemesCatalog(out Returns ret)
        {
            List<Sprav> themes = null;
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Supg cli = new cli_Supg(WCFParams.AdresWcfHost.CurT_Server);
                themes = cli.GetThemesCatalog(out ret);
            }
            else
            {
                Supg db = new Supg();
                try
                {
                    themes = db.GetThemesCatalog(out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции GetThemesCatalog");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV:" + "() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return themes;
        }

        /// <summary>
        /// Изменить справочник Классификация сообщений
        /// </summary>
        /// <returns>результат</returns>
        public bool UpdateThemesCatalog(Sprav finder, out Returns ret)
        {
            bool res = false;
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Supg cli = new cli_Supg(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.UpdateThemesCatalog(finder, out ret);
            }
            else
            {
                Supg db = new Supg();
                try
                {
                    res = db.UpdateThemesCatalog(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции UpdateThemesCatalog");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV:" + "() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return res;
        }

    }

}

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel.Configuration;
using Bars.KP50.Utils;
using STCLINE.KP50.Client;
using STCLINE.KP50.REPORT;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Server
{
    public class srv_ExcelRep : srv_Base, I_ExcelRep
    {
        /// <summary>
        /// Расчет задолженности по оплате за ЖКУ по адресу
        /// </summary>
        /// <param name="finder">Параметры отчета</param>
        /// <returns>Результат</returns>
        public Returns GetCalcAddressDeptReport(Dept finder)
        {
            Returns ret = new Returns(true);

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_ExcelRep cli = new cli_ExcelRep(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetCalcAddressDeptReport(finder);
            }
            else
            {
                using (ExcelRep db = new ExcelRep())
                {
                    try
                    {
                        ret = db.GetCalcAddressDeptReport(finder);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                        {
                            MonitorLog.WriteLog("Ошибка GetCalcAddressDeptReport() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                        }
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Получить ключи адреса (nzp_town, nzp_raj, nzp_ul, nzp_dom) по ключу nzp_kvar
        /// </summary>
        /// <param name="finder">Параметры отчета</param>
        /// <returns>Результат</returns>
        public Returns GetAddressID(int nzp_kvar, out Ls ls)
        {
            Returns ret = new Returns(true);
            ls = new Ls();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_ExcelRep cli = new cli_ExcelRep(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetAddressID(nzp_kvar, out ls);
            }
            else
            {
                using (ExcelRep db = new ExcelRep())
                {
                    try
                    {
                        ret = db.GetAddressID(nzp_kvar, out ls);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                        {
                            MonitorLog.WriteLog("Ошибка GetAddressID() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                        }
                    }
                }
            }
            return ret;
        }
        
        //Генерация отчета Лицевые счета + домовые или квартирные параметры(Самара)
        public Returns CreateExcelReport_host(List<Prm> listprm, int nzp_user, string comment)
        {
            Returns ret = Utils.InitReturns();
            var srvAdr = new srv_Adres();

            //Создание таблицы tXX_prmall
            ret = srvAdr.Generator(listprm, nzp_user);
            if (!ret.result)
            {
                ret.text = "Создание таблиц в БД ОШИБКА!";
                return ret;
            }

            //Генерация отчета Лицевые счета + домовые или квартирные параметры(Самара)
            var ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            var container = new ParamContainer();
            container.listprm = listprm;
            container.nzp_user = nzp_user;
            container.comment = comment;
            //System.Threading.Thread currentT = null;

            ////опрос очереди, ставим в очереди
            //if ( Constants.ExcelQueue.Count != 0)
            //{
            //    //запись в очередь очередного задания
            //    Constants.ExcelQueue.Enqueue(container);

            //    return ret;
            //} 

            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReportGenerator.LsKvarDomParam));
                    Constants.ExcelThreads[i].IsBackground = true;
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;

                }
            }

            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);

                ////становимся в очередь
                //Constants.ExcelQueue.Enqueue(container);
                ////запускаем поток опрашивающий пул на свободное место
                //Constants.QThreadExcel = new System.Threading.Thread(new System.Threading.ThreadStart(this.CheckPoolThreadsExcel));
                //Constants.QThreadExcel.IsBackground = true;
                //Constants.QThreadExcel.Start();

            }
            return ret;

        }

        //Отчет: Сальдовая оборотная ведомость
        public Returns GetSaldoServices(SupgFinder finder, int supp)
        {
            Returns ret = Utils.InitReturns();
            var ReportGenerator = new ReportGen();
            var container = new ParamContainer { finder = finder, nzp_supp = supp };

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReportGenerator.GetSaldoServices));
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        //отчет по начислениям по поставщикам
        public Returns GetNachSupp(int supp, SupgFinder finder, int yearr, bool serv)
        {
            Returns ret = Utils.InitReturns();
            var ReportGenerator = new ReportGen();
            var container = new ParamContainer { nzp_supp = supp, finder = finder, yearr = yearr, serv = serv };

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReportGenerator.GetNachSupp));
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        //отчет : Реестр счетчиков по лицевым счетам
        public Returns GetRegisterCounters(SupgFinder finder)
        {
            Returns ret = Utils.InitReturns();
            var reportGenerator = new ReportGen();
            var container = new ParamContainer { finder = finder, nzp_user = finder.nzp_user };

            //System.Threading.Thread currentT = null;
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetRegisterCounters);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }
            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        //Отчет: сверка расчетов с жильцом по состоянию
        public Returns GetVerifCalcs(Ls finder, string yy_from, string mm_from, string yy_to, string mm_to)
        {
            Returns ret = Utils.InitReturns();
            var reportGenerator = new ReportGen();
            var container = new ParamContainer
            {
                finder = finder,
                from_mm = mm_from,
                from_yy = yy_from,
                to_mm = mm_to,
                to_yy = yy_to
            };

            //System.Threading.Thread currentT = null;
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetVerifCalcs);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }


            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }


        //Отчет: состояние жилого фонда по приборам учета за период
        public Returns GetStateGilFond(string yy_from, string mm_from, string yy_to, string mm_to)
        {
            Returns ret = Utils.InitReturns();
            var reportGenerator = new ReportGen();
            var container = new ParamContainer();

            container.from_mm = mm_from;
            container.from_yy = yy_from;
            container.to_mm = mm_to;
            container.to_yy = yy_to;
            //System.Threading.Thread currentT = null;
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetVerifCalcs);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }


            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        //Отчет: справка для предъявления в суд
        public Returns GetDebtCalcs(Ls finder, string yy_from, string mm_from, string yy_to, string mm_to)
        {
            Returns ret = Utils.InitReturns();
            var reportGenerator = new ReportGen();
            var container = new ParamContainer
            {
                finder = finder,
                from_mm = mm_from,
                from_yy = yy_from,
                to_mm = mm_to,
                to_yy = yy_to
            };

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetDebtCalcs);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        //Отчет: извещение за месяц
        public Returns GetNoticeCalcs(Ls finder, string yy, string mm)
        {
            Returns ret = Utils.InitReturns();
            var reportGenerator = new ReportGen();
            var container = new ParamContainer { finder = finder, yy = yy, mm = mm };

            //System.Threading.Thread currentT = null;
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetNoticeCalcs);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }


            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }


        //Отчет: список заявок
        public Returns GetOrderList(int nzp_user)
        {
            Returns ret = Utils.InitReturns();
            var reportGenerator = new ReportGen();
            var container = new ParamContainer { nzp_user = nzp_user };

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetOrderList);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }

            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }


        //Отчет: список плановых работ
        public Returns GetPlannedWorksList(int nzp_user, enSrvOper en)
        {
            Returns ret = Utils.InitReturns();
            var reportGenerator = new ReportGen();
            var container = new ParamContainer();
            container.nzp_user = nzp_user;
            container.en = en;
            bool addToThread = false;

            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetPlannedWorksList);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }

            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        //Отчет: по списку заявок
        public Returns GetSupgReports(SupgFinder finder, enSrvOper en)
        {
            Returns ret = Utils.InitReturns();
            var reportGenerator = new ReportGen();
            var container = new ParamContainer
            {
                nzp_user = finder.nzp_user,
                to_mm = finder._date_to,
                from_mm = finder._date_from,
                en = en
            };
            bool addToThread = false;

            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetSupgReports);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }

            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }


        //Отчет: отчет по количеству заявлений, направленных по услугам за период
        public Returns GetCountOrders(Ls finder, string _nzp, string _nzp_add, string s_date, string po_date)
        {
            Returns ret = Utils.InitReturns();
            var reportGenerator = new ReportGen();
            var container = new ParamContainer
            {
                finder = finder,
                nzp_user = finder.nzp_user,
                from_mm = s_date,
                to_mm = po_date,
                _nzp = _nzp,
                _nzp_add = _nzp_add
            };

            //System.Threading.Thread currentT = null;
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetCountOrders);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }

            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        //Отчет: список заявок
        public Returns GetIncomingJobOrders(int nzp_user)
        {
            Returns ret = Utils.InitReturns();
            var reportGenerator = new ReportGen();
            var container = new ParamContainer { nzp_user = nzp_user };

            //System.Threading.Thread currentT = null;
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetIncomingJobOrders);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }

            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        //Отчет: список недопоставок
        public Returns GetRepNedopList(int nzp_user, int nzp_jrn)
        {
            Returns ret = Utils.InitReturns();
            var reportGenerator = new ReportGen();
            var container = new ParamContainer { nzp_user = nzp_user, _nzp = nzp_jrn.ToString(CultureInfo.InvariantCulture) };

            //System.Threading.Thread currentT = null;
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetRepNedopList);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }

            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        //Отчет: оплата гражданами-получателями коммунальных услуг за поставленные услуги
        public Returns GetDeliveredServicesPayment(Ls finder, int nzp_supp, string yy, string mm)
        {
            Returns ret = Utils.InitReturns();
            var reportGenerator = new ReportGen();
            var container = new ParamContainer
            {
                finder = finder,
                nzp_user = finder.nzp_user,
                yy = yy,
                mm = mm,
                nzp_supp = nzp_supp
            };

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetDeliveredServicesPayment);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        //Отчет: получение начислений по дому
        public Returns GetDomCalcs(int Nzp_user, string mm, string yy)
        {
            Returns ret = Utils.InitReturns();
            var db = new DBCalcs();
            db.FindCalcs(Nzp_user, yy, mm, out ret, "");
            db.Close();
            var ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            var container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.mm = mm;
            container.yy = yy;
            System.Threading.Thread currentT = null;
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetDomCalcs);
                    currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }


            if (addToThread)
            {
                //ожидание завершения отчета
                while (currentT.IsAlive)
                {
                    System.Threading.Thread.Sleep(5);
                }
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: карта аналитического учета
        public Returns GetAnalisKart(List<Prm> listprm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            var reportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = listprm;
            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetAnalisKart);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        //Отчет: калькуляция тарифа содержание жилья
        public Returns GetCalcTarif(Prm prm, int Nzp_user)
        {

            Returns ret = Utils.InitReturns();

            var reportGenerator = new ReportGen();

            //постановка задания в пул потоков
            var container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);
            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetCalcTarif);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        //Отчет: калькуляция тарифа содержание жилья
        public Returns GetAllAgrementsReport(DateTime? dat_s, DateTime? dat_po, int user, int area)
        {
            Returns ret = Utils.InitReturns();

            var reportGenerator = new ReportGen();

            //постановка задания в пул потоков
            var container = new ParamContainer { nzp_user = user };
            var prm = new Prm { nzp_area = area };
            if (dat_s == null) prm.dat_s = "";
            else prm.dat_s = dat_s.ToDateTime().ToShortDateString();
            if (dat_po == null) prm.dat_po = "";
            else prm.dat_po = dat_po.ToDateTime().ToShortDateString();


            container.listprm = new List<Prm>();
            container.listprm.Add(prm);
            //System.Threading.Thread currentT = null;
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetAllAgrementsReport);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }


            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        //Отчет: Расшифровка по домам начислено
        public Returns GetDomNach(List<Prm> listprm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            var reportGenerator = new ReportGen();

            //постановка задания в пул потоков
            var container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = listprm;
            //System.Threading.Thread currentT = null;
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetDomNach);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }


            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        //Отчет: Сальдовая оборотная ведомость
        public Returns GetSaldoRep10_14_3(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            var reportGenerator = new ReportGen();

            //постановка задания в пул потоков
            var container = new ParamContainer { nzp_user = Nzp_user, listprm = new List<Prm> { prm_ } };
            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetSaldoRep10_14_3);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        //Отчет: Сальдовая оборотная ведомость
        public Returns GetSaldoRep10_14_1(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            var reportGenerator = new ReportGen();

            //постановка задания в пул потоков
            var container = new ParamContainer { nzp_user = Nzp_user, listprm = new List<Prm> { prm_ } };
            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetSaldoRep10_14_1);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Сверка поступлений за день
        public Returns GetSverkaDay(ExcelSverkaPeriod prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            var reportGenerator = new ReportGen();

            //постановка задания в пул потоков
            var container = new ParamContainer
            {
                nzp_user = Nzp_user,
                listsverkaperiod = new List<ExcelSverkaPeriod> { prm_ }
            };
            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetSverkaDay);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        public Returns GetDataSaldoPoPerechisl(MoneyDistrib finder, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            var reportGenerator = new ReportGen();

            //постановка задания в пул потоков
            /*   ParamContainer container = new ParamContainer();
               container.nzp_user = Nzp_user;          
               container.MoneyDistrib = finder;*/


            try
            {
                string fileName = "";
                ret = reportGenerator.GetSaldoPere(finder, ref fileName);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка GetReportDicts " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                ret = new Returns(false, ex.Message);
            }



            //System.Threading.Thread currentT = null;
            /*   bool AddToThread = false;
               for (int i = 0; i < Constants.ExcelThreads.Length; i++)
               {
                   if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                   {
                       Constants.ExcelThreads[i] = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReportGenerator.GetSaldoPere));
                       //currentT = Constants.ExcelThreads[i];
                       Constants.ExcelThreads[i].IsBackground = true;
                       Constants.ExcelThreads[i].Start(container);
                       AddToThread = true;
                       break;
                   }
               }


               if (AddToThread)
               {
                   ////ожидание завершения отчета
                   //while (currentT.IsAlive)
                   //{
                   //    System.Threading.Thread.Sleep(5);
                   //}
               }
               else
               {
                   ret.result = false;
                   ret.text = "Все потоки заняты, повторите операцию позже.";
                   MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
               }*/


            return ret;
        }

        //Отчет: Сверка поступлений за месяц
        public Returns GetSverkaMonth(ExcelSverkaPeriod prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            var reportGenerator = new ReportGen();

            //постановка задания в пул потоков
            var container = new ParamContainer
            {
                nzp_user = Nzp_user,
                listsverkaperiod = new List<ExcelSverkaPeriod> { prm_ }
            };
            //System.Threading.Thread currentT = null;
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetSverkaMonth);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }


            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Начисления за месяц по поставщику
        public Returns GetCharges(Prm prm_, int Nzp_user)
        {

            Returns ret = Utils.InitReturns();

            var reportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm_);
            //System.Threading.Thread currentT = null;
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetCharges);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }


            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Расшифровка по домам начислено
        public Returns GetDomNachPere(List<Prm> listprm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            var reportGenerator = new ReportGen();

            //постановка задания в пул потоков
            var container = new ParamContainer { nzp_user = Nzp_user, listprm = listprm };
            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetDomNachPere);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //достать DataTable: получение начислений по дому
        public DataTable GetDomCalcs_table(int Nzp_user, string mm, string yy)
        {
            Returns ret;
            var db = new ExcelRep();
            DataTable dt = db.GetCalcs(out ret, Nzp_user.ToString(), mm, yy);
            db.Close();
            if (ret.result)
            {
                return dt;
            }
            else
            {
                return null;
            }
        }

        //Отчет: справка по поставщикам 
        public Returns GetSpravSuppNach(List<Prm> listprm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            var reportGenerator = new ReportGen();

            //постановка задания в пул потоков
            var container = new ParamContainer { nzp_user = Nzp_user, listprm = listprm };
            //System.Threading.Thread currentT = null;
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetSpravSuppNach);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }


            if (addToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        //Отчет: справка по поставщикам  с характеристиками
        public Returns GetSpravSuppNachHar(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            var reportGenerator = new ReportGen();

            //постановка задания в пул потоков
            var container = new ParamContainer { nzp_user = Nzp_user };
            var p = new List<Prm>();
            p.Add(prm_);
            container.listprm = p;
            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetSpravSuppNachHar);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: справка по поставщикам форма 3
        public Returns GetSpravSuppNachHar2(Prm prm_, int Nzp_user)
        {

            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            List<Prm> p = new List<Prm>();
            p.Add(prm_);
            container.listprm = p;
            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravSuppNachHar2);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: справка о наличие задолженности
        public Returns GetSpravHasDolg(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.nzp_kvar = prm_.nzp_kvar;
            container.mm = prm_.month_.ToString();
            container.yy = prm_.year_.ToString();
            container.finder = prm_;

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravHasDolg);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: справка о лицевом счете
        public Returns GetLicSchetExcel(Ls finder, int year_, int month_)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = finder.nzp_user;
            container.nzp_kvar = finder.nzp_kvar;
            container.mm = month_.ToString();
            container.yy = year_.ToString();
            container.comment = finder.pref + " " + finder.pm_note;

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetLicSchetExcel);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Акт сверки по энергосбыту
        public Returns GetEnergoActSverki(Prm prm_, int nzp_supp, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            List<Prm> p = new List<Prm>();
            p.Add(prm_);
            container.listprm = p;
            container.nzp_supp = nzp_supp;

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetEnergoActSverki);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: справка о наличие задолженности
        public Returns GetSpravPULs(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();

            container.nzp_user = Nzp_user;
            List<Prm> p = new List<Prm>();
            p.Add(prm_);
            container.listprm = p;
            container.mm = prm_.month_.ToString();
            container.yy = prm_.year_.ToString();

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravPULs);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Ведомость оплат по ЛС
        public Returns GetVedOplLs(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetVedOplLs);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Ведомость перерасчетов
        public Returns GetVedPere(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetVedPere);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Сведения о просроченной задолженности
        public Returns GetDolgSved(Prm prm, int Nzp_user)
        {

            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetDolgSved);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Список должников более трех месяцев
        public Returns GetDolgSpis(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetDolgSpis);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Список рассогласований с паспортисткой
        public Returns GetPaspRas(Prm prm, int Nzp_user)
        {

            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetPaspRas);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        //Отчет: рассогласований с паспортисткой для г.Губкина
        public Returns GetPaspRasCommon(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetPaspRasCommon);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }



        //Отчет: Список должников более трех месяцев
        public Returns GetSostGilFond(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSostGilFond);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Сводная ведомость нормативов потребления
        public Returns GetVedNormPotr(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);
            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetVedNormPotr);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        //Отчет: Формирование квитанций
        public Returns GetFakturaFiles(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetFakturaFiles);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        //Отчет: Справка о начислении платы по виду услуги содержание жилья
        public Returns GetSpravSoderg(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravSoderg);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Справка о начислении платы по виду услуги форма 2
        public Returns GetSpravSoderg2(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    if (prm.nzp_serv == 8)
                        Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravSoderg2Heat);
                    else
                        Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravSoderg2Water);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }

            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Справка по услугам группы Содержание жилья
        public Returns GetSpravGroupSodergGil(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravGroupSodergGil);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Справка по отключениям подачи коммунальных услуг
        public Returns GetSpravPoOtklUslug(Ls finder, int nzp_serv, int month, int year)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = finder.nzp_user;
            container.mm = month.ToString();
            container.yy = year.ToString();
            container.nzp_serv = nzp_serv;

            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravPoOtklUslug);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Справка по отключениям подачи коммунальных услуг по домам с указанием виновника
        public Returns GetSpravPoOtklUslugDomVinovnik(Prm prm)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = prm.nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);


            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravPoOtklUslugDomVinovnik);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Справка по поставщикам коммунальных услуг ф.3
        public Returns GetSpravSuppSamara(Prm prm)
        {

            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = prm.nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);


            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravSuppSamara);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Справка по отключениям подачи коммунальных услуг по домам с указанием виновника
        public Returns GetSpravPoOtklUslugDom(Prm prm)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = prm.nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);


            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravPoOtklUslugDom);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Справка по отключениям подачи коммунальных услуг по домам с указанием виновника
        public Returns GetSpravPoOtklUslugGeuVinovnik(Prm prm)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = prm.nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);


            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravPoOtklUslugGeuVinovnik);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        /// <summary>
        /// Отчет:50.1 Сальдовая ведомость по энергосбыту
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetSaldoVedEnergo(Prm prm)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = prm.nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);


            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSaldoVedEnergo);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        /// <summary>
        /// Отчет:Список временно зарегистрированных
        /// </summary>
        public Returns GetVremZareg(Kart finder)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = finder.nzp_user;
            container.Kartfinder = finder;


            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetVremZareg);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        /// <summary>
        /// Отчет:Список для военкомата
        /// </summary>
        public Returns GetVoenkomat(Kart finder)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = finder.nzp_user;
            container.Kartfinder = finder;


            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetVoenkomat);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        /// <summary>
        /// Отчет:50.2 Ведомость должников
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetDolgSpisEnergo(Prm prm)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = prm.nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);


            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetDolgSpisEnergo);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        /// <summary>
        /// Отчет:3.70 Отчет по начислениям поставщиков для Тулы
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetServSuppNach(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = prm.nzp_user;
            container.reportPrm = prm;


            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetServSuppNach);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        /// <summary>
        /// Отчет:3.71 Отчет по поступлениям платежей для Тулы
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetServSuppMoney(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = prm.nzp_user;
            container.reportPrm = prm;



            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetServSuppMoney);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }



        /// <summary>
        /// Отчет: Список домов с количеством распечанных счетов
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetListDomFaktura(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = prm.nzp_user;
            container.reportPrm = prm;


            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetListDomFaktura);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        /// <summary>
        /// Отчет: Протокол сверки данных
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetProtocolSverData(Finder finder)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = finder.nzp_user;
            container.Finder = finder;

            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetProtocolSverData);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        /// <summary>
        /// Отчет: Протокол сверки данных лицевых счетов и домов
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetProtocolSverDataLsDom(Prm p)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = p.nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(p);

            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetProtocolSverDataLsDom);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }

            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        /// <summary>
        /// Отчет: Информация по расчетам с начелением
        /// </summary>
        /// <param name="finder">Объект поиска типа Ls</param>
        /// <param name="month">Текущий месяц</param>
        /// <param name="year">Текущий год</param>
        /// <returns>Объект-результат Returns</returns>
        public Returns GetInfPoRaschetNasel(Ls finder, int nzp_supp, int month, int year)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.finder = finder;
            container.nzp_user = finder.nzp_user;
            container.mm = month.ToString();
            container.yy = year.ToString();
            container.nzp_supp = nzp_supp;

            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetInfPoRaschetNasel);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        /// <summary>
        /// Получение данных Отчет: Генератор по начислениям
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <param name="par">Список параметров-начислений которых необходимо вывести</param>
        /// <param name="month">Текущий месяц</param>
        /// <param name="year">Текущий год</param>
        /// <returns>объект Returns</returns>
        public Returns GetReportPrmNach(Ls finder, List<int> par, int month, int year, string comment, List<int> services, bool isShowExpanded)
        {
            Returns ret = Utils.InitReturns();

            //Создание таблицы tХХХ_saldoall
            /*
            srv_Adres srvAdr = new srv_Adres();
            bool resGenerate = srvAdr.GenerateSaldoAll(Constants.cons_Webdata, Constants.cons_Kernel, par, finder.nzp_user, year, month, out ret); //в каком вормате месяц год?!!!
            if (!resGenerate)
            {
                ret.result = false;
                ret.text = "Создание таблицы tХХХ_saldoall в БД ОШИБКА!";
                return ret;
            }
            */

            DbGenerator db_gen = new DbGenerator();
            ret = db_gen.GenCharge(par, services, finder.nzp_user, year, month, true, isShowExpanded);
            db_gen.Close();



            //Генерация отчета Лицевые счета + домовые или квартирные параметры(Самара)
            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.finder = finder;
            container.parList = par;
            container.mm = month.ToString();
            container.yy = year.ToString();
            container.comment = comment;

            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    //формировать в развернутом виде
                    if (isShowExpanded)
                    {
                        Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetReportPrmNach_2);
                    }
                    //в обычном
                    else
                    {
                        Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetReportPrmNach);
                    }
                    Constants.ExcelThreads[i].IsBackground = true;
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;

                }
            }

            if (AddToThread)
            {
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);

            }
            return ret;
        }

        /// <summary>
        /// Отчет "Контроль распределения оплат"
        /// </summary>        
        public Returns GetControlDistribPayments(Payments pay)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.payments = pay;

            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetControlDistribPayments);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Протокол рассчитанных значений ОДН
        public Returns GetProtCalcOdn(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetProtCalcOdn);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        //Отчет: Протокол рассчитанных значений ОДН расширенный
        public Returns GetProtCalcOdn2(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = Nzp_user;
            container.listprm = new List<Prm>();
            container.listprm.Add(prm);

            //System.Threading.Thread currentT = null;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetProtCalcOdn2);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {
                ////ожидание завершения отчета
                //while (currentT.IsAlive)
                //{
                //    System.Threading.Thread.Sleep(5);
                //}
            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        // Выгрузка сальдо в банк
        //----------------------------------------------------------------------
        public Returns GetSaldo_v_bank(SupgFinder finder, string year, string month, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.supgfinder = finder;
            container.yy = year;
            container.mm = month;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSaldo_v_bank);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        // Загрузка адресного пространства из КЛАДР
        //----------------------------------------------------------------------
        public Returns UploadKLADRAddrSpace(KLADRFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.KLADRfinder = finder;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.UploadKLADRAddrSpace);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        // Выгрузка файла обмена
        //----------------------------------------------------------------------
        public Returns GenerateExchange(SupgFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.supgfinder = finder;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GenerateExchange);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        // Выгрузка начислений УЭС
        //----------------------------------------------------------------------
        public Returns GenerateUESVigr(SupgFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.supgfinder = finder;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GenerateUESVigr);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        // Выгрузка оплат МУРЦ
        //----------------------------------------------------------------------
        public Returns GenerateMURCVigr(SupgFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.supgfinder = finder;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GenerateMURCVigr);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        // Выгрузка начислений
        //----------------------------------------------------------------------
        public Returns GetUploadCharge(SupgFinder finder, string year, string month, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.supgfinder = finder;
            container.yy = year;
            container.mm = month;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetUploadCharge);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        // Выгрузка показаний ПУ
        //----------------------------------------------------------------------
        public Returns GetUploadPU(SupgFinder finder, string year, string month, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.supgfinder = finder;
            container.yy = year;
            container.mm = month;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetUploadPU);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        //Смена УК для ЛС
        //----------------------------------------------------------------------
        public Returns ChangeArea(FinderChangeArea finder)
        //----------------------------------------------------------------------
        {
            Returns ret;
            ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.ChangeAreaFinder = finder;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.ChangeArea);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        // Выгрузка реестра для загрузки в БС
        //----------------------------------------------------------------------
        public Returns GetUploadReestr(Finder finder, List<int> BanksList, string unloadVersionFormat, string statusLS, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.Finder = finder;
            container.parList = BanksList;//список банков по которым производится выгрузка
            container.unloadVersionFormat = unloadVersionFormat;
            container.comment = statusLS;

            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetUploadReestr);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        // Выгрузка сальдо в банк
        //----------------------------------------------------------------------
        public Returns GetUploadKassa(SupgFinder finder, string year, string month, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.supgfinder = finder;
            container.yy = year;
            container.mm = month;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetUploadKassa);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }
            
            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        /// <summary>
        /// Выгрузка по принятым для перечисления денежным средствам    
        /// </summary>
        /// <param name="finder">Параметры выгрузки</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Returns GetChargeUnload(ChargeUnloadPrm finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.chargeUnloadPrm = finder;
            
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetChargeUnload);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }



        public Returns LoadOneTime(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.filesImportedFinder = finder;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.LoadOneTime);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }


        //----------------------------------------------------------------------
        public Returns GetSaldo_5_10(ChargeFind finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.ChargeFind = finder;

            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSaldo_5_10);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        // Выгрузка для обмена с соц.защитой (Тула)  
        public Returns GetExchangeSZ(Finder finder, string year, string month, int nzp_ex_sz, bool isPkodInLs)
        //-----------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.Finder = finder;
            container.yy = year;
            container.mm = month;
            container.nzp = nzp_ex_sz;
            container.serv = isPkodInLs;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetExchangeSZ);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }


        // Загрузка для обмена с соц.защитой (Тула)  
        public Returns GetUploadExchangeSZ(Finder finder, string filename, string fileNameFull, string encodingValue, List<int> listWP)
        //-----------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            var reportGenerator = new ReportGen();

            var container = new ParamContainer { Finder = finder, yy = filename, from_yy = fileNameFull, mm = encodingValue, parList = listWP };
            bool addToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(reportGenerator.GetUploadExchangeSZ)
                    {
                        IsBackground = true
                    };
                    Constants.ExcelThreads[i].Start(container);
                    addToThread = true;
                    break;
                }
            }

            if (!addToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        // Прием оплат от Банка
        public Returns UploadReestrInFon(FilesImported finder)
        //-----------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.filesImportedFinder = finder;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.UploadReestrInFon);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }

            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }


        //----------------------------------------------------------------------
        public Returns GetProtocolVTB24(ExFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.ExFinder = finder;

            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetProtocolVTB24);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }
            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }


        #region отчеты для системы должников

        //напоминание
        public string GetReminderToDebitor(Deal finder, ReportType type, out Returns ret)
        //-----------------------------------------------
        {
            string str;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_ExcelRep(WCFParams.AdresWcfHost.CurT_Server);
                str = cli.GetReminderToDebitor(finder, type, out ret);
            }
            else
            {
                var db = new ReportGen();
                str = db.GetBlankPDF(finder, type, 2, out ret);
            }
            return str;
        }


        //уведомление
        public string GetNoticeToDebitor(Deal finder, ReportType type, out Returns ret)
        //-----------------------------------------------
        {
            string str;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_ExcelRep(WCFParams.AdresWcfHost.CurT_Server);
                str = cli.GetNoticeToDebitor(finder, type, out ret);
            }
            else
            {
                var db = new ReportGen();
                str = db.GetBlankPDF(finder, type, 1, out ret);
            }
            return str;
        }

        //предупреждение
        public string GetWarningToDebitor(Deal finder, ReportType type, out Returns ret)
        //-----------------------------------------------
        {
            string str;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_ExcelRep(WCFParams.AdresWcfHost.CurT_Server);
                str = cli.GetWarningToDebitor(finder, type, out ret);
            }
            else
            {
                var db = new ReportGen();
                str = db.GetBlankPDF(finder, type, 3, out ret);
            }
            return str;
        }

        #endregion


        #region Отчеты для ТУЛЫ

        /// <summary>
        /// Отчет: Сводный отчет по принятым и перечисленным средствам для Тулы
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetServSuppMoney2(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = prm.nzp_user;
            container.reportPrm = prm;



            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetServSuppMoney2);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }



        /// <summary>
        /// Отчет: Справка по должникам по Туле
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetSpravDolgTula(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = prm.nzp_user;
            container.reportPrm = prm;



            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravDolgTula);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }


        /// <summary>
        /// Отчет по должникам по Туле
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetListDolgTula(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = prm.nzp_user;
            container.reportPrm = prm;



            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetListDolgTula);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }

        /// <summary>
        /// Отчет справка по поставщикам Тула
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetSpravSuppTula(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();

            ReportGen ReportGenerator = new ReportGen();

            //постановка задания в пул потоков
            ParamContainer container = new ParamContainer();
            container.nzp_user = prm.nzp_user;
            container.reportPrm = prm;



            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.GetSpravSuppTula);
                    //currentT = Constants.ExcelThreads[i];
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }


            return ret;
        }



        #endregion



        #region Процедура опроса пула на свободное место
        //========= (запускается в отдельном потоке и выполняется пока очередь на выполнение отчета не опустеет)=======        
        private void CheckPoolThreadsExcel() // СДЕЛАТЬ УНИВЕРСАЛЬНОЙ!!!
        {
            ReportGen ReportGenerator = new ReportGen();

            while (Constants.ExcelQueue.Count != 0)
            {
                //выбираем свободные потоки
                var FreeThreads = (from th in Constants.ExcelThreads where !th.IsAlive select th).ToArray<System.Threading.Thread>();
                if (FreeThreads != null && FreeThreads.Length != 0)
                {
                    //вытаскиваем из очереди по максимуму и ставим на выполнение
                    for (int i = 0; i < FreeThreads.Length; i++)
                    {
                        if (Constants.ExcelQueue.Count != 0)
                        {
                            FreeThreads[i] = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReportGenerator.LsKvarDomParam));
                            FreeThreads[i].IsBackground = true;
                            FreeThreads[i].Start(Constants.ExcelQueue.Dequeue());
                        }
                    }
                }
                else
                {
                    //ждем 5 секунд
                    System.Threading.Thread.Sleep(5000);
                }
            }
        }
        //=============================================================================================================
        #endregion

        #region Передача файла по WCF
        public byte[] GetFile(string path)
        {
            byte[] b = null;
            try
            {
                FileInfo fi = new FileInfo(path);
                b = new byte[fi.Length];

                FileStream fs = fi.OpenRead();
                fs.Read(b, 0, b.Length);
                fs.Flush();
                fs.Close();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка преобразования файла в байты : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }


            return b;
        }
        #endregion

        #region универсальный сервер отчетов

        /// <summary>
        /// получение данных справочников для универсального сервера отчета
        /// </summary>
        /// <param name="idDicts">список идентификаторов справочников</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Dict> GetReportDicts(List<int> idDicts, bool loadDictsData, out Returns ret)
        {
            ExcelRep db = new ExcelRep();
            List<Dict> res = null;
            try
            {
                res = db.GetReportDicts(idDicts, loadDictsData, out ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка GetReportDicts " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                ret = new Returns(false, ex.Message);
            }
            db.Close();
            return res;
        }

        #endregion

        #region Обмен со сторонними поставщиками

        // Выгрузка ЛС и адресов
        public Returns FileSyncLS(ExFinder finder)
        //-----------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.ExFinder = finder;


            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.FileSyncLS);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }


        // Выгрузка изменений параметров ЛС
        public Returns FileChangeLS(ExFinder finder)
        //-----------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.ExFinder = finder;


            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.FileChangeLS);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        #endregion

        public Returns StartTransfer(TransferParams finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            var ReportGenerator = new ReportGen();

            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.StartTransfer);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(finder);
                    AddToThread = true;
                    break;
                }
            }
            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }


        public Returns UploadVTB24(FilesImported finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            var ReportGenerator = new ReportGen();

            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(ReportGenerator.UploadVTB24);
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(finder);
                    AddToThread = true;
                    break;
                }
            }
            if (!AddToThread)
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using IBM.Data.Informix;
using System.IO;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Timers;
using System.Reflection;

namespace ReportServer
{
    /// <summary>
    /// класс для работы с сервером отчетов
    /// </summary>
    public static class ReportManager
    {
        public static string sSource = "ReportServerSource";
        public static string sLog = "ReportServerLog";
        public static string connWeb = "";
        public static string connKernel = "";
        public static string exportPath = "";
        public static string templatePath = "";
        public static string ipString = "";//ip-адрес
        public static int port = 0;//порт
        public static int isAlive = 0;//признак работоспособности сервера отчетов
        public static int requestTime = 10;// время между запросами (сек.)

        /// <summary>
        /// старт сервера отчетов
        /// </summary>
        public static void StartReportServer()
        {
            System.Timers.Timer tmr = new System.Timers.Timer();
            //работа с журналом ошибок
            if (!EventLog.SourceExists(sSource))
                EventLog.CreateEventSource(sSource, sLog);

            EventLog.WriteEntry(sSource, "Запуск логики сервера отчетов", EventLogEntryType.Information);
            Console.WriteLine("Запуск сервера отчета...");
            Console.WriteLine("Время запуска: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));

            ReportParams currentReport = null;
            Thread tr = null;
            try
            {
                //получение настроек для пула потоков
                //пока по умолчанию
                ThreadPool threadPool = new ThreadPool(20);
                //считываем конфиг файлы и заполняем строку соединения
                Dictionary<string, string> conf = ReadConfigFiles();
                if (conf.Count > 0)
                {
                    Console.WriteLine("Чтение файлов настроек...");
                    connWeb = conf.FirstOrDefault(x => x.Key == "W1").Value;
                    port = Convert.ToInt32(conf.FirstOrDefault(x => x.Key == "W2").Value);
                    ipString = conf.FirstOrDefault(x => x.Key == "W3").Value;
                    connKernel = conf.FirstOrDefault(x => x.Key == "W4").Value;
                    Constants.cons_Kernel = connKernel;
                    Constants.cons_Webdata = connWeb;
                    EventLog.WriteEntry(sSource, @conf.FirstOrDefault(x => x.Key == "W6").Value + @"reports\", EventLogEntryType.Information);
                    exportPath = @conf.FirstOrDefault(x => x.Key == "W6").Value + @"reports\";
                    templatePath = conf.FirstOrDefault(x => x.Key == "W5").Value;
                    //считывание порта и айпи

                    bool isExists = System.IO.Directory.Exists(exportPath);
                    if (!isExists)
                        System.IO.Directory.CreateDirectory(exportPath);
                    Console.WriteLine("Чтение конфигурационных файлов прошло успешно...");

                    #region заполнение необходимых параметров программы Комплат
                    _DBReport db = new _DBReport();
                    Points.Pref = db.IfmxGetPref(connKernel);
                    Returns returns = Utils.InitReturns();
                    if (!db.PointLoad(sSource, out returns))
                    {
                        EventLog.WriteEntry(sSource, "Ошибка при заполнении префиксов БД!", EventLogEntryType.Error);
                        return;
                    }
                    #endregion

                    #region загрузка всех dll отчетов

                    Dictionary<int, string> dic = db.GetDllNames(connWeb);
                    ReportVersionControl.AddReportImpl(dic);

                    #endregion

                    #region запуск клиента сокета

                    tr = new Thread(() => StartSocket());
                    tr.Start();
                    #endregion

                    while (true)
                    {
                        try
                        {
                            ReturnsObjectType<List<ReportParams>> ret = new _DBReport().GetReportList(new IfxConnection(connWeb), new IfxConnection(connKernel), exportPath, templatePath);
                            List<ReportParams> newReports = new List<ReportParams>();
                            if (ret.returnsData != null)
                                newReports = ret.returnsData;
                            else
                            {
                                EventLog.WriteEntry(sSource, "Ошибка получения списка отчетов на выполнение!", EventLogEntryType.Error);
                                return;
                            }

                            //обновление статусов отчетов в действующей таблице отчетов
                            foreach (ReportParams rep in newReports)
                            {
                                EventLog.WriteEntry(sSource, "Постановка в очередь на выполнение отчета: " + rep.name + "...", EventLogEntryType.Information);
                                Console.WriteLine("Постановка в очередь на выполнение отчета: " + rep.name + "...");
                                _DBReport dbr = new _DBReport();
                                dbr.UpdateReportStatusOldTable(rep, 1, ReportManager.connWeb, true);
                            }

                            if (newReports.Count > 0)
                            {
                                //логика постановки отчета в очередь на выполенение
                                foreach (ReportParams report in newReports)
                                {
                                    string dllName = db.CheckReportForUpdate(report.id, connWeb);
                                    if (!String.IsNullOrEmpty(dllName))
                                        ReportVersionControl.AddReportImpl(report.id, dllName);
                                    report.work = new Action<ReportParams>(PrepareReport.Prepare);
                                    currentReport = report;
                                    //запуск отчета
                                    threadPool.Execute(report);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ReportManager.isAlive = 1;


                            EventLog.WriteEntry(sSource, "Ошибка работы сервера отчетов: " + ex.InnerException.Message, EventLogEntryType.Error);
                            break;
                        }
                        Thread.Sleep(5000);
                    }
                }
                else
                {
                    ReportManager.isAlive = 1;


                    EventLog.WriteEntry(sSource, "Ошибка сервера отчетов: конфиг файл пуст!", EventLogEntryType.Error);
                }
            }
            catch (Exception ex)
            {
                ReportManager.isAlive = 1;

                if (currentReport == null)
                    EventLog.WriteEntry(sSource, "Ошибка сервера отчета: " + ex.Message, EventLogEntryType.Error);
                else
                    EventLog.WriteEntry(sSource, "Ошибка сервера отчета при работе с отчетом: " +
                        currentReport.name + ", id(" + currentReport.id + ")" + ex.Message, EventLogEntryType.Error);
            }
            finally
            {
                //очистка потоисполняющихся потоков
            }
        }

        /// <summary>
        /// чтение конфигурационного файла
        /// </summary>
        public static Dictionary<string, string> ReadConfigFiles()
        {
            return ConfigLoad.GetValuesFromConfig(@"C:\reportServer\Host.config");
        }

        /// <summary>
        /// Отсылает запросы по socket
        /// </summary>
        /// <param name="port">port</param>
        public static void StartSocket()
        {          
            try
            {
                    // Буфер для входящих данных
                    byte[] bytes = new byte[1024];

                    // Устанавливаем удаленную точку для сокета
                    IPHostEntry ipHost = Dns.GetHostEntry("localhost");
                    IPAddress ipAddr = ipHost.AddressList[0];
                    IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, ReportManager.port);

                    string message = "";
                   
                    while (true)
                    {
                        Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        System.Threading.Thread.Sleep((1000) * (ReportManager.requestTime));

                        // Соединяем сокет с удаленной точкой
                        sender.Connect(ipEndPoint);

                        //Console.WriteLine("Сокет соединяется с {0} ", sender.RemoteEndPoint.ToString());

                        message = ReportManager.isAlive.ToString();    

                        byte[] msg = Encoding.UTF8.GetBytes(message);

                        // Отправляем данные через сокет
                        int bytesSent = sender.Send(msg);
                        //// Получаем ответ от сервера
                        //int bytesRec = sender.Receive(bytes);

                        bytes = null;
                        // Освобождаем сокет
                        sender.Shutdown(SocketShutdown.Both);
                        sender.Close();
                    }
            }
            catch (Exception ex)
            {
                ReportManager.isAlive = 1;
                EventLog.WriteEntry(sSource, "Ошибка на сервере отчетов", EventLogEntryType.Error);
                Console.WriteLine(ex.ToString());
            }
        }
    }
}

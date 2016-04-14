using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ReportServer;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.IO;

namespace ReportServerConsole
{
    class Program
    {
        public static int failCount = 0;//колличество падений сервера.
        public static bool bTime = false; // состояние stopwatch
        public static long duration = 0;//колличество секунд после первого падения серва.
        public static int limitTime = 180000; //предельное время для предотвращения исключения.(миллисекунды)
        public static string sSource = "ReportServerSource";
        
        static void Main(string[] args)
        {
            //Console.ReadKey();
            string sSource = "ReportServerSource";
            string sLog = "ReportServerLog";

            try
            {
                //работа с журналом ошибок
                if (!EventLog.SourceExists(sSource))
                    EventLog.CreateEventSource(sSource, sLog);

                EventLog.WriteEntry(sSource, "Запуск сервиса ReportService для запуска сервера отчетов ReportServer", EventLogEntryType.Information);

                #region Запускаем поток с Sockert Listener
                Thread trlis = new Thread(() => ListenSocket());
                trlis.Start();
                #endregion
                ReportManager.StartReportServer();
            }
            catch (Exception)
            {
                EventLog.WriteEntry(sSource, "Запуск сервиса для запуска сервера отчётов не удался", EventLogEntryType.Information);
            }
        }

        static void ListenSocket()
        {
            Dictionary<string, string> conf = ReportManager.ReadConfigFiles();
            int port = Convert.ToInt32(conf.FirstOrDefault(x => x.Key == "W2").Value);
            int PID = -1;
            // Устанавливаем для сокета локальную конечную точку
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);

            Stopwatch stopWatch = new Stopwatch();

            // Создаем сокет Tcp/Ip
            Socket sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // Назначаем сокет локальной конечной точке и слушаем входящие сокеты
                sListener.Bind(ipEndPoint);
                sListener.Listen(10);
                // Начинаем слушать соединения
                while (true)
                {
                    //Console.WriteLine("Ожидаем соединение через порт {0}", ipEndPoint);

                    // Программа приостанавливается, ожидая входящее соединение
                    Socket handler = sListener.Accept();

                    string data = null;
                    // Мы дождались клиента, пытающегося с нами соединиться
                    byte[] bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);

                    data += Encoding.UTF8.GetString(bytes, 0, bytesRec);

                    // Показываем данные на консоли
                    if (data == "0")
                    {
                        //Console.Write("Сервер отчетов работает: " + data + "\n\n");
                        Program.bTime = stopWatch.IsRunning;
                        if (Program.bTime)
                        {
                            stopWatch.Stop();
                            stopWatch.Reset();
                            Program.duration = stopWatch.ElapsedMilliseconds;
                        }
                    }
                    else
                    {
                        Console.Write("Сервер отчетов упал: " + data + "\n\n");
                        EventLog.WriteEntry(sSource, "Сервер отчетов выдал ошибку !", EventLogEntryType.Error);
                        Program.failCount++;
                        Program.bTime = stopWatch.IsRunning;

                        if (!Program.bTime)
                        {
                            stopWatch.Start();
                        }
                        Program.duration = stopWatch.ElapsedMilliseconds;

                        if ((Program.failCount >= 5) && (Program.duration < Program.limitTime))
                        {
                            EventLog.WriteEntry(sSource, "Сервер отчетов в течении " + Program.limitTime / 60000 + " минут выдал исключение " + Program.failCount + " раз, производится остановка программы !", EventLogEntryType.Error);
                            handler.Shutdown(SocketShutdown.Both);
                            handler.Close();
                            Environment.Exit(0);
                        }
                        else
                        {
                            Thread reportThread = new Thread(() => ReportManager.StartReportServer());
                            reportThread.Start();
                            ReportManager.isAlive = 0;
                        }
                    }
                    if (Program.duration >= 180000)
                    {
                        if (Program.failCount > 5) Program.failCount = 0;
                    }
                    #region ответ клиенту
                    // string message = "Запрос принят";
                    // byte[] msg = Encoding.UTF8.GetBytes(message);
                    //// Отправляем ответ клиенту\
                    // handler.Send(msg);
                    #endregion
                    if (data.IndexOf("<TheEnd>") > -1)
                    {
                        Console.WriteLine("Сервер завершил соединение с клиентом.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ReadLine();
            }
        }
    }
}

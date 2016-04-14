




namespace STCLINE.KP50.Updater
{
    class Program
    {       
        static void Main(string[] args)
        {

            #region Для дебага (раскомментировать и назначить запускаемым проектом)
            //// здесь надо запустить updater.exe с параметрами "Строка подключения, id процесса, логин, пароль, путь к папке с распакованным обновлением, путь к папке для обновления, индекс обновления (0-хост, 1-веб, 2-брокер), восстановить из резервной копии (0 - нет, 1 - да)"
            //args = new string[1];
            //args[0] = "Client Locale=ru_ru.CP1251;Database=websmr3;Database Locale=ru_ru.915;Server=ol_mars;UID = informix;Pwd = info".Replace(' ', '€') +
            //      "♂-1" +
            //      "♂Administrator" +
            //      "♂rubin" +
            //      "♂C:\\_1\\1\\host" +
            //      "♂C:\\webkp5" +
            //      "♂0" +
            //      "♂0";
            #endregion

            Updater UPD = new Updater(args);

            UPD.CreateTimer();

            while (!UPD.UpdateEnd)
            {
                System.Threading.Thread.Sleep(3000);
            }                     
        }       
    }
}

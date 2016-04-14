using System;
using Bars.KP50.SzExchange.UnloadForSZ;
using STCLINE.KP50.Global;
using STCLINE.KP50.Utility;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Функция обработки нажатия кнопки "Выгрузить"
    /// </summary>
    public class StartUnl : BaseUnloadClass
    {
        private string exchangeWithErc_v22 = "Выгрузка в ППП Социальная защита 2.0";
        private string residentRegister = "Состав проживающих граждан";
        private string exchangeWithErc_v22Log = "Выгрузка в ППП Социальная защита 2.0";
        private string residentRegisterLog = "Состав проживающих граждан";

        public Returns Run(FilesImported finder)
        {
            Returns ret = new Returns(true);
            MonitorLog.WriteLog("Старт выгрузки в СЗ" , MonitorLog.typelog.Info, true);
            if (finder.bank == "") return new Returns(false, "Не выбран банк данных для выгрузки");
            

            #region Сохранение записи о выгрузке в базу данных
                       

            //создаем вспомогательный контейнер для работы с ExcelUtility
            var excUtility = new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = String.Format("{0} за {1}.{2}",exchangeWithErc_v22, finder.month, finder.year),
                is_shared = 1
            };

            //формируем полный путь к файлу выгрузки
            exchangeWithErc_v22 = CreateFileFullName(exchangeWithErc_v22 + DateTime.Now.ToString("yyyyMMddHHmmss"));
            exchangeWithErc_v22Log = CreateFileFullName(exchangeWithErc_v22Log + DateTime.Now.ToString("yyyyMMddHHmmss")+".Log");
            try
            {
                //записываем в таблицу excel_utility
                var myFiles = new DBMyFiles();
                //считываем присвоенный код в таблице excel_utility
                finder.nzp_exc = excUtility.nzp_exc = myFiles.AddFile(excUtility).tag;

                //если ошибка при записи - выходим
                if (!ret.result) return ret;


                #endregion Сохранение записи о выгрузке в базу данных



                UnlHead uhd = new UnlHead();
                UnlHousehold uh = new UnlHousehold();
                UnlPassport up = new UnlPassport();
                UnlListOfSupp uls = new UnlListOfSupp();
                UnlRecalcServ urs = new UnlRecalcServ();

                
                //Заголовок
                finder.saved_name = exchangeWithErc_v22;
                uhd.Start(finder);

                SetProcessProgress(0.1m, excUtility.nzp_exc);

                //Домохозяйства
                finder.saved_name_log = exchangeWithErc_v22Log;
                uh.Start(finder);

                SetProcessProgress(0.8m, excUtility.nzp_exc);

                //Список поставщиков
                uls.Start(finder);

                //Паспортистка
                residentRegister = CreateFileFullName("P0020" + finder.nzp_exc);
                residentRegisterLog = CreateFileFullName("P0020" + finder.nzp_exc + ".Log");
                finder.saved_name_log = residentRegisterLog;
                finder.saved_name = residentRegister;
                up.Start(finder);

                //архивация
                string outputArchiveName = null;
                Archive.GetInstance()
                    .Compress((outputArchiveName = exchangeWithErc_v22 + ".zip"),
                        new[] { exchangeWithErc_v22, exchangeWithErc_v22Log, residentRegister, residentRegisterLog }, true);

                //сохранение на ftp
                if (InputOutput.useFtp)
                    excUtility.exc_path = InputOutput.SaveOutputFile(outputArchiveName);
                
                СhangeProcessStatus(1, ExcelUtility.Statuses.Success, excUtility);
            }
            catch (Exception ex)
            {
                СhangeProcessStatus(0, ExcelUtility.Statuses.Failed, excUtility);
                ret.result = false;
                ret.text = "Ошибка! Текст ошибки: " + ex.Message;
                return ret;
            }

            ret.text = "Успешно!";
            return ret;
        }

        private string CreateFileFullName(string shortName)
        {
            return String.Format("{0}{1}{2}",
                    InputOutput.GetOutputDir(),
                    shortName,
                    ".txt"
                    );
        }

        public override void CreateTempTable()
        {
            throw new NotImplementedException();
        }

        public override void Start()
        {
            throw new NotImplementedException();
        }
        public override void Start(FilesImported finder)
        {
            throw new NotImplementedException();
        }
    }
}


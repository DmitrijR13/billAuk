using System.Data.SqlClient;
using System.Security.Principal;
using System.Text;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Класс для взаимодействия с СЗ (загрузка/выгрузка)
    /// </summary>
    public class DbLoadFileFromSZ : DataBaseHeadServer
    {
        //контейнер параметров
        private FilesImported _finder;

        //StringBuilder для записи текста ошибок
        private StringBuilder err = new StringBuilder();

        /// <summary>
        /// Ф-ция загрузки файла из СЗ
        /// </summary>
        /// <param name="finder">контейнер входящих пеерменных</param>
        /// <param name="ret">переменная для возврата результата работы ф-ции</param>
        /// <returns></returns>
        public Returns LoadFileFromSz(FilesImported finder, ref Returns ret)
        {
            _finder = finder;
            
            //записываем в "Мои файлы"
            int nzpExc = DbFileLoader.AddMyFile("Загрузка из СЗ", _finder);

            //разархивируем файл
            string _fullFileName = DbFileLoader.DecompressionFile(_finder.saved_name, InputOutput.GetInputDir(), ".txt", ref ret);


            DbFileLoader fl = new DbFileLoader(ServerConnection)
            {
                _fDirectory = InputOutput.GetInputDir(),
                _finder = this._finder
            };

            //записываем в files_imported и получаем уникальный код загрузки
            //передаем тип файла 2 - загрузка из СЗ
            fl._finder.nzp_file = _finder.nzp_file = fl.InsertIntoFiles_imported(ref ret);

            //Выставление статуса успешной загрузки файла
            fl.SaveAndSetStat(nzpExc, ref ret);

            //считывание файла в массив строк и передача в ф-цию
            ret = Run(DbFileLoader.ReadFile(_fullFileName));
            

            return ret;
        }

        /// <summary>
        /// Ф-ция обработки
        /// </summary>
        /// <param name="fileStrings"></param>
        /// <returns></returns>
        private Returns Run(string[] fileStrings)
        {
            Returns ret = new Returns();
            ret = Utils.InitReturns();

            //предварительные проверки входных параметров
            CheckInputPrms();

            //предварительные проверки файла
            Check();


            return ret;
        }

        private Returns Check()
        {
            return new Returns(true, "Результат проверки");
        }
        private Returns CheckInputPrms()
        {
            return new Returns(true, "Результат проверки");
        }
    }
}

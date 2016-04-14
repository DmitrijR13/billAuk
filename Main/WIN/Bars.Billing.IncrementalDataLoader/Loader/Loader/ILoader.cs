using System.Collections.Generic;

namespace Bars.Billing.IncrementalDataLoader.Loader
{
    interface ILoader
    {
        /// <summary>
        /// Получить форматы загрузки
        /// </summary>
        /// <returns></returns>
        List<AssemblyAttribute> GetFormats();
        /// <summary>
        /// Получить форматы загрузки(для Веба)
        /// </summary>
        /// <returns></returns>
        List<AssemblyAtr> GetFormatsForWeb();
        /// <summary>
        /// Запустить 
        /// </summary>
        /// <param name="request"></param>
        Returns Start(Request request);
        /// <summary>
        /// Удалить по ключу из БД
        /// </summary>
        /// <param name="nzp_load"></param>
        /// <returns></returns>
        Returns Delete(int nzp_load, ConfigurationParams config = null);
        /// <summary>
        /// Данные таблицы imports
        /// </summary>
        /// <returns></returns>
        List<Request> GetImportValues(ConfigurationParams config = null);
        /// <summary>
        /// Обновление данных в таблице imports
        /// </summary>
        /// <param name="reqList"></param>
        /// <returns></returns>
        List<Request> UpdateImportValue(List<Request> reqList, ConfigurationParams config = null);
        /// <summary>
        /// Добавить запись в таблицу imports
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        Request Insert(Request req, ConfigurationParams config = null);
        /// <summary>
        /// Проверка на существование пользователя в таблице users
        /// </summary>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        User CheckUser(string login, string password, ConfigurationParams config = null);
        /// <summary>
        /// Получение схем БД
        /// </summary>
        /// <returns></returns>
        List<string> GetSchemas(ConfigurationParams config = null);

        /// <summary>
        /// Сохранить пользователя
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        User SaveUser(User user, ConfigurationParams config = null);

        void StartOperation(List<ConfigurationParams> confParams, List<OtherParams> parms);

        void StartOperationPGU(ConfigurationParams confParams, List<OtherParams> parms);
    }
}

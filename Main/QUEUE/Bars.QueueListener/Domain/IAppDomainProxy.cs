namespace Bars.QueueListener.Domain
{
    using System;
    using System.Collections.Generic;

    /// <summary>Интерфейс, определяющий контракт доменного прокси</summary>
    public interface IAppDomainProxy : IDisposable
    {
        /// <summary>Загрузка списка сборок</summary>
        /// <param name="assemblies">Список полных имен файлов загружаемых сборок</param>
        /// <returns>true or false</returns>
        bool LoadAssembles(IEnumerable<string> assemblies);

        /// <summary>Загружка сборки в новый или существующий домен</summary>
        /// <param name="assemblyPath">Полный путь файла сборки</param>        
        /// <returns>true or false</returns>
        bool LoadAssembly(string assemblyPath);

        /// <summary>Выполнение функции в домене</summary>
        /// <typeparam name="TResult">Тип возвращаемого результата</typeparam>
        /// <typeparam name="TArguments">Тип аргументов вызова</typeparam>
        /// <param name="fn">Функция, выполняемая в домене</param>
        /// <param name="arguments">Аргументы вызова</param>
        /// <returns>Результат выполнения</returns>
        TResult Execute<TResult, TArguments>(Func<TArguments, TResult> fn, TArguments arguments);

        /// <summary>Выполнение процедуры в домене</summary>
        /// <typeparam name="TArguments">Тип аргументов вызова</typeparam>
        /// <param name="fn">Процедура, выполняемая в домене</param>
        /// <param name="arguments">Аргументы вызова</param>
        void Execute<TArguments>(Action<TArguments> fn, TArguments arguments);
    }
}
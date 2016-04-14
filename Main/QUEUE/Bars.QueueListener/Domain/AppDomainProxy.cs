namespace Bars.QueueListener.Domain
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>Прокси класс домена приложения, работающий в новом домене</summary>
    public class AppDomainProxy : MarshalByRefObject, IAppDomainProxy
    {
        private IList<string> _loadedAssemblies = new List<string>();

        ~AppDomainProxy()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool LoadAssembles(IEnumerable<string> assemblies)
        {
            return assemblies.Select(LoadAssembly).ToArray().All(success => success);
        }

       public bool LoadAssembly(string assemblyPath)
        {
            // если файла сборки не существует, возвращаем false
            if (!File.Exists(assemblyPath))
            {
                return false;
            }

            var path = assemblyPath.ToLower();

            // если сборка уже загружена, возвращаем true
            if (_loadedAssemblies.Contains(path))
            {
                return true;
            }

            // загружаем сборку в домен
            try
            {
                var content = File.ReadAllBytes(path);
                //Assembly.LoadFrom(path);
                Assembly.Load(content);
                _loadedAssemblies.Add(path);

                return true;
            }
            catch
            {
            }

            return false;
        }

        public virtual TResult Execute<TResult, TArguments>(Func<TArguments, TResult> fn, TArguments arguments)
        {
            return fn.Invoke(arguments);
        }

        public virtual void Execute<TArguments>(Action<TArguments> fn, TArguments arguments)
        {
            fn.Invoke(arguments);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _loadedAssemblies == null)
            {
                return;
            }

            _loadedAssemblies.Clear();
            _loadedAssemblies = null;
        }
    }
}
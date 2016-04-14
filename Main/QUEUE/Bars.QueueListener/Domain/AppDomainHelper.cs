namespace Bars.QueueListener.Domain
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    /// <summary>Класс отвечающий за создание и уничтожение доменов приложения</summary>
    public class AppDomainHelper : IDisposable
    {
        private readonly Dictionary<string, AppDomain> _mapDomains = new Dictionary<string, AppDomain>();
        private readonly Dictionary<string, IAppDomainProxy> _domainProxies = new Dictionary<string, IAppDomainProxy>();

        ~AppDomainHelper()
        {
            Dispose(false);
        }

        /// <summary>Создание нового домена</summary>
        /// <param name="domainName">Наименование создаваемого домена</param>
        /// <param name="binPath">Путь до библиотек</param>
        /// <returns>Прокси домена</returns>
        public IAppDomainProxy CreateDomain(string domainName, string binPath)
        {            
            if (_domainProxies.ContainsKey(domainName))
            {
                return _domainProxies[domainName];
            }

            var domainProxyType = typeof(AppDomainProxy);

            var setup = new AppDomainSetup
            {
                PrivateBinPath = binPath,
                ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                LoaderOptimization = LoaderOptimization.MultiDomainHost
                //ShadowCopyDirectories = shadowCopyDirectories
            };

            // иначе создадим новый домен
            var appDomain = AppDomain.CreateDomain(domainName, AppDomain.CurrentDomain.Evidence, setup);
            /*
            var folder = new DirectoryInfo(binPath);

            var libraries = folder.GetFiles("*.dll")                    .Select(x => AssemblyName.GetAssemblyName(x.FullName));

            var loadedAssemblies = appDomain.GetAssemblies().Select(x => x.GetName()).ToArray();

            foreach (var asmName in libraries)
            {
                if (loadedAssemblies.Any(x => x.FullName == asmName.FullName))
                {
                    continue;
                }

                appDomain.Load(asmName.FullName);
            }*/

            _mapDomains[domainName] = appDomain;
            
            var proxy = appDomain.CreateInstanceFrom(domainProxyType.Assembly.Location, domainProxyType.FullName).Unwrap();

            var domainProxy = (IAppDomainProxy)proxy;
            _domainProxies[domainName] = domainProxy;
            return domainProxy;
        }        

        /// <summary>Выгрузка домена</summary>
        /// <param name="domainName">Наименование домена</param>
        /// <returns>true or false</returns>
        public bool UnloadDomain(string domainName)
        {
            // проверяем что передано имя домена
            if (string.IsNullOrEmpty(domainName))
            {
                return false;
            }

            // проверяем что домен зарегистрирован
            if (!this._mapDomains.ContainsKey(domainName))
            {
                return false;
            }

            try
            {
                var appDomain = this._mapDomains[domainName];

                // выгружаем домен
                AppDomain.Unload(appDomain);

                // удаляем запись о домене
                this._mapDomains.Remove(domainName);
                this._domainProxies.Remove(domainName);
            }
            catch (Exception exc)
            {
                //Здесь нужно будет залогировать ошибку
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var appDomain in _mapDomains.Values)
                {
                    AppDomain.Unload(appDomain);
                }

                _mapDomains.Clear();
            }
        }
    }
}
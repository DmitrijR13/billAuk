using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Exchange.TransferHouses
{
    using System;
    using Castle.MicroKernel.Registration;
    using Castle.Windsor;


    /// <summary>Установщик переноса таблиц</summary>
    public class TransferObjectsInstaller
    {
        public TransferObjectsInstaller()
        {
            Container = new WindsorContainer();
            Container.Kernel.Resolver.AddSubResolver(new ListResolver(Container.Kernel, true));
        }

        private readonly Object lock_item = new Object();

        public IWindsorContainer Container { get; set; }
        public decimal koeff = 1;

        /// <summary>Зарегистрировать</summary>
        public void Register()
        {
            var type = typeof(Transfer);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(x => type.IsAssignableFrom(x) && !x.IsAbstract &&
                        (x.IsDefined(typeof(TransferAttributes), true) && x.GetCustomAttributes(typeof(TransferAttributes), true)
                            .Cast<TransferAttributes>()
                            .Single()
                            .Enabled)).ToList();
            types =
                types.OrderBy(
                    x =>
                        (int)x.GetCustomAttributes(typeof(TransferAttributes), true)
                            .Cast<TransferAttributes>()
                            .Single()
                            .Priority).ToList();
            types.ForEach(x =>
                Container.Register(
                    Component
                        .For<Transfer>()
                        .ImplementedBy(x)
                        .Named(null)
                        .LifestyleTransient())
            );
        }

        public List<Transfer> Resolve()
        {
            return (Container.ResolveAll(typeof(Transfer))).OfType<Transfer>().ToList();
        }

        public T Resolve<T>()
        {
            return Container.Resolve<T>();
        }

        

        /// <summary>
        /// Проверка структуры таблиц
        /// </summary>
        /// <param name="transfer_id">Идентификатор переноса</param>
        /// <returns></returns>
        public bool Comparer(int transfer_id, string fpoint, string tpoint, ref List<string> commList)
        {
            var objList = Resolve();
            try
            {
                foreach (var obj in objList)
                {
                    obj.StartCompare();
                }
            }
            catch
            {
                //Пишем комментарий в лог
                string message = String.Format("Ошибка переноса домов из банка '{0}' в банк '{1}' " +
                                               "на этапе проверки структуры таблиц (разные структуры таблиц), обратитесь к разработчику.", fpoint, tpoint);
                commList.Add(message);

                TransferProgress.UpdateProgress(transfer_id, 4);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Запуск переноса 
        /// </summary>
        /// <param name="transfer_id">Идентификатор переноса</param>
        /// <param name="ExecuteTransfer">Список перенесенных домов</param>
        /// <returns></returns>
        public bool TransferExecute(int transfer_id, string fpoint, string tpoint, out List<Transfer> ExecuteTransfer, ref List<string> commList)
        {
            ExecuteTransfer = new List<Transfer>();
            var objList = Resolve();
            try
            {
                for (var i = 0; i < objList.Count; i++)
                {
                    if (objList[i] != null)
                    {
                        lock (lock_item)
                            ExecuteTransfer.Add(objList[i]);
                        objList[i].StartTransfer();
                        var t = objList[i].GetType();
                        TransferAttributes attribute =
                            (TransferAttributes) Attribute.GetCustomAttribute(t, typeof (TransferAttributes));
                        string mess = String.Format("Этап '{1}' таблицы '{0}' завершен.", attribute.Name, attribute.Descr);
                        commList.Add(mess);
                    }
                    TransferProgress.UpdateProgress(objList.Count != 0 ? koeff * (i + 1) / (objList.Count * 2) : 0, transfer_id, 1);
                }
            }
            catch
            {
                foreach (var obj in Enumerable.Reverse(ExecuteTransfer))
                {
                    obj.StartAddedRollback();
                }
                var t = ExecuteTransfer.Last().GetType();
                TransferAttributes attribute =
                            (TransferAttributes)Attribute.GetCustomAttribute(t, typeof(TransferAttributes));
                var name = attribute.Name;
                //Пишем комментарий в лог
                string message = String.Format("Ошибка при переносе домов из банка '{0}' в банк '{1}' " +
                                               "на этапе '{2}' таблицы '{3}', обратитесь к разработчику.", 
                                               fpoint, tpoint, attribute.Descr, name);
                commList.Add(message);

                TransferProgress.UpdateProgress(transfer_id, 3);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Запуск удаления из старых таблиц
        /// </summary>
        /// <param name="transfer_id">Идентификатор переноса</param>
        /// <param name="ExecuteTransfer">Список перенесенных домов</param>
        /// <returns></returns>
        public bool DeleteExecute(int transfer_id, string fpoint, List<Transfer> ExecuteTransfer, ref List<string> commList)
        {
            try
            {
                for (var i = ExecuteTransfer.Count - 1; i >= 0; i--)
                {
                    ExecuteTransfer[i].StartDelete();
                    TransferProgress.UpdateProgress(ExecuteTransfer.Count != 0 ? koeff * (i + 1) / (ExecuteTransfer.Count * 2) : 0, transfer_id, 1);
                }
            }
            catch
            {
                //Пишем комментарий в лог
                string message = String.Format("Ошибка при переносе домов из банка '{0}' в банк '{1}' " +
                                               "на этапе удаления данных '" + ExecuteTransfer.Last().ToString() + "', обратитесь к разработчику.", fpoint);
                commList.Add(message);
                foreach (var obj in ExecuteTransfer)
                {
                    obj.StartDeletedRollback();
                    obj.StartAddedRollback();
                }
                TransferProgress.UpdateProgress(transfer_id, 3);
                return false;
            }
            return true;
        }
    }
}
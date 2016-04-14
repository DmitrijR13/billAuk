using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bars.Security.Extentions
{
    /// <summary>
    /// Singleton
    /// </summary>
    /// <typeparam name="T">Изолируемый тип</typeparam>
    public class Singleton<T> where T : class
    {
        private sealed class SingletonCreator<I> where I : class
        {
            /// <summary>
            /// Сзоданный экземпляр объекта
            /// </summary>
            private static I instance = null;

            /// <summary>
            /// При необходимости создает и возвращает созданный объект
            /// Вызывает приватный конструктор без параметров
            /// </summary>
            public static I Instance
            {
                get
                {
                    return instance ?? (instance =
                        typeof(I).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                        Type.EmptyTypes, null).Invoke(null) as I);
                }
            }
        }

        /// <summary>
        /// Возвращает экземпляр объекта
        /// </summary>
        public static T Instance { get { return SingletonCreator<T>.Instance; } }
    }
}

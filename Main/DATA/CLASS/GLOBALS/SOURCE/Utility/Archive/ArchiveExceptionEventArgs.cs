using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STCLINE.KP50.Utility
{
    /// <summary>
    /// Аргументы для события Exception в архиваторе
    /// </summary>
    public class ArchiveExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Свойство типа Exception
        /// </summary>
        public Exception InheritException { get; set; }

        /// <summary>
        /// конструктор для ArchiveExceptionEventArgs
        /// </summary>
        public ArchiveExceptionEventArgs(Exception ex = null)
        {
            InheritException = ex;
        }

        /// <summary>
        /// Перегрузка ToString для свойства InheritException
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return InheritException.ToString();
        }
    }
}

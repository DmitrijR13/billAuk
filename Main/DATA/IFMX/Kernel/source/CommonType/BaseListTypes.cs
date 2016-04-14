using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace STCLINE.KP50.DataBase
{
        /// <summary>
        /// Типы баз данных
        /// </summary>
        public enum BaselistTypes
        {
            /// <summary>
            /// Начисления
            /// </summary>
            Charge = 1,

            /// <summary>
            /// Характеристики жилья и т.п.
            /// </summary>
            Data = 2,

            /// <summary>
            /// Системный
            /// </summary>
            Kernel = 3,

            /// <summary>
            /// Финансовый
            /// </summary>
            Fin = 4,

            Tbo = 5,
            Cds = 6,
            Mail = 7,
            WebFon = 8,
            WebCds = 9,

            /// <summary>
            /// Основной банк данных (используется в банке-клоне)
            /// </summary>
            PrimaryBank = 10
        }
    
}

using System;
using System.Runtime.Serialization;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Globals.SOURCE.GLOBAL
{
    /// <summary>
    /// Класс для параметров проверок
    /// </summary>
    [Serializable]
    public class CheckBeforeClosingParams
    {
        /// <summary>
        /// Параметры пользователя
        /// </summary>
        [DataMember]
        public User User { set; get; }

        /// <summary>
        /// Параметры банка данных
        /// </summary>
        [DataMember]
        public _Point Bank { set; get; }

    }

}

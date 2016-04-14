using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Bars.KP50.Load.Obninsk.CountersLoad.Interfaces
{
    /// <summary>
    /// Интерфейс для объекта периода перерасчета
    /// </summary>
    public interface IMustCalc:IDisposable
    {
        void Init(IDbConnection connection, string sourceTable, int nzp_user);
        /// <summary>
        /// Подготовка периодов перерасчета
        /// </summary>
        /// <param name="pref"></param>
        void PrepareGroupMustcalc(string pref);
        /// <summary>
        /// Сохранение периодов перерасчета
        /// </summary>
        void SetGroupMustCalc(string comment);
    }
}

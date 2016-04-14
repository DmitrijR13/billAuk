using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Bars.KP50.Load.Obninsk.CountersLoad.Interfaces
{
    /// <summary>
    /// Интерфес для определения максимального допустимого отклонения между текущими и предыдущими показаниями
    /// </summary>
    public interface IPermissibleDiffValCounters
    {
        void Init(IDbConnection connection, List<int> nzp_wp_list);
        /// <summary>
        /// Получить значение отклонения
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="nzpServ"></param>
        /// <returns></returns>
        Decimal GetDiffByServ(string pref, int nzpServ);
        /// <summary>
        /// Получить наименование услуги
        /// </summary>
        /// <param name="nzpServ"></param>
        /// <returns></returns>
        string GetServName(int nzpServ);
    }
}

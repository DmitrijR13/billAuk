using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Text;
using System.Collections.Generic;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Серверный класс для работы с формой Сальдо по перечислениям по договорам
    /// </summary>
    public static class DbFinUtils
    {
        public static DateTime GetOperDay()
        {
            using (var db = new DbPack())
            {
                Returns ret;
                return db.GetOperDay(out ret);
            }
        }
    }
}

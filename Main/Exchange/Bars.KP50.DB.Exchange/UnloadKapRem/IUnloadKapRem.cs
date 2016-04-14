using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Interfaces
{
    public interface IUnloadKapRem
    {
        /// <summary>Имя файла выгрузки</summary>
        string FileName { get; set; }

        /// <summary>Старт выгрузки </summary>
        Returns StartUnloadKapRem(out Returns ret, int nzpUser, string year, string month, string pref);

        /// <summary>Создать временные таблицы</summary>
        void CreateTempTableKapRem();

        /// <summary>Удалить временные таблицы</summary>
        void DropTempTableKapRem();
    }
}

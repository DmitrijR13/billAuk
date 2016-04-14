using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Archive
    {
        /// <summary>
        /// Делает архив БД
        /// </summary>
        /// <param name="finder">Указывается ID пользователя </param>
        /// <returns>Результат операции</returns>
        [OperationContract]
        Returns MakeArchive(Finder finder);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using STCLINE.KP50.Global;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Collections;
using System.IO;
namespace Bars.KP50.CLI.Exchange.Interface
{
    [ServiceContract]
    public interface I_UnlPassport
    {
        /// <summary>
        /// Повторная выгрузка файла для СЗ
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        [OperationContract]
        Returns RepeatedlyUnload(FilesImported finder);

        
    }

    public interface IUnlPassportRemoteObject : I_UnlPassport, IDisposable { }
}

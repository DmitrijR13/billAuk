using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace STCLINE.KP50.Utility
{
    public static class ConvertUtility
    {
        /// <summary>
        /// сериализация объекта в строку
        /// </summary>
        /// <param name="obj">сериализуемый объект</param>
        /// <returns></returns>
        public static string ObjectToString(object obj)
        {
            MemoryStream memorystream = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(memorystream, obj);
            return Convert.ToBase64String(memorystream.ToArray());
        }


        /// <summary>
        /// десериализация объекта из строки
        /// </summary>
        /// <param name="base64String">строка для сериализации</param>
        /// <returns>object</returns>
        public static object StringToObject(string base64String)
        {
            byte[] bytes = Convert.FromBase64String(base64String);
            MemoryStream ms = new MemoryStream(bytes, 0, bytes.Length);
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;
            return new BinaryFormatter().Deserialize(ms);
        }

    }
}

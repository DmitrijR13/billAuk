using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Bars.Security.Security.Cryptography
{
    /// <summary>
    /// Провайдер криптографии для алгоритма MD5
    /// </summary>
    public class Md5CryptoProvider : CryptoProvider
    {
        /// <summary>
        /// Вычисляет хэш заданной последовательности байт
        /// </summary>
        /// <param name="bytes">Хэшируемые исходные данные</param>
        /// <returns>Хэшированные данные</returns>
        public override string ComputeHash(byte[] bytes)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(bytes);
                var output = new StringBuilder(32);
                foreach (var character in hash)
                    output.Append(character.ToString("X2"));
                return output.ToString();
            }
        }
    }
}

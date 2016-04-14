using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Security.Cryptography
{
    /// <summary>
    /// Провайдер криптографии
    /// </summary>
    public interface ICryptoProvider
    {
        /// <summary>
        /// Вычисляет хэш заданной последовательности байт
        /// </summary>
        /// <param name="bytes">Хэшируемые исходные данные</param>
        /// <returns>Хэшированные данные</returns>
        string ComputeHash(byte[] bytes);

        /// <summary>
        /// Вычисляет хэш заданной последовательности байт
        /// </summary>
        /// <param name="data">Хэшируемые исходные данные</param>
        /// <returns>Хэшированные данные</returns>
        string ComputeHash(string data);

        /// <summary>
        /// Шифрует заданную последовательность байт
        /// </summary>
        /// <param name="bytes">Шифруемые исходные данные</param>
        /// <returns>Шифрованные данные</returns>
        string Encrypt(byte[] bytes);

        /// <summary>
        /// Дешифрует заданную последовательность байт
        /// </summary>
        /// <param name="bytes">Дешифруемые исходные данные</param>
        /// <returns>Деифрованные данные</returns>
        byte[] Decrypt(string data);
    }
}

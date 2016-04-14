using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Security.Cryptography
{
    /// <summary>
    /// Базовый абстрактный класс для реализации криптопровайдеров
    /// </summary>
    public abstract class CryptoProvider : ICryptoProvider
    {
        /// <summary>
        /// Вычисляет хэш заданной последовательности байт
        /// </summary>
        /// <param name="bytes">Хэшируемые исходные данные</param>
        /// <returns>Хэшированные данные</returns>
        public virtual string ComputeHash(byte[] bytes)
        {
            throw new NotSupportedException("Этот алгоритм не поддерживает хэширование");
        }

        /// <summary>
        /// Вычисляет хэш заданной последовательности байт
        /// </summary>
        /// <param name="data">Хэшируемые исходные данные</param>
        /// <returns>Хэшированные данные</returns>
        public string ComputeHash(string data)
        {
            byte[] bytes = Encoding.Default.GetBytes(data);
            return ComputeHash(bytes);
        }

        /// <summary>
        /// Шифрует заданную последовательность байт
        /// </summary>
        /// <param name="bytes">Шифруемые исходные данные</param>
        /// <returns>Шифрованные данные</returns>
        public virtual string Encrypt(byte[] bytes)
        {
            throw new NotSupportedException("Этот алгоритм не поддерживает шифрование");
        }

        /// <summary>
        /// Дешифрует заданную последовательность байт
        /// </summary>
        /// <param name="bytes">Дешифруемые исходные данные</param>
        /// <returns>Деифрованные данные</returns>
        public virtual byte[] Decrypt(string data)
        {
            throw new NotSupportedException("Этот алгоритм не поддерживает дешифрование");
        }
    }
}

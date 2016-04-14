using Bars.Security.Authentication.Session;
using Bars.Security.Exceptions.Authentication;
using Bars.Security.Exceptions.Security;
using Bars.Security.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Bars.Security.Security.Authentication
{
    /// <summary>
    /// Генератор и валидатор токенов.
    /// Структура токена: UserId [8 bytes]|GenerationTime [8 bytes]|ExpireTime [8 bytes]|Reserved [24 bytes]|Random [8 bytes]|Checksum [8 bytes]
    /// Алгоритм шифрования: AES
    /// </summary>
    internal static class Token
    {
        /// <summary>
        /// Время валидности токена.
        /// </summary>
        public static TimeSpan ValidTime { get { return TimeSpan.FromMinutes(1); } }

        /// <summary>
        /// Базовый размер токена (байт)
        /// </summary>
        private static int TokenLength { get { return 64; } }

        /// <summary>
        /// Копирует субмассив из массива
        /// </summary>
        /// <typeparam name="T">Базовый тип массива</typeparam>
        /// <param name="array">Исходный массив</param>
        /// <param name="offset">Количество пропускаемых элементов</param>
        /// <param name="length">Длина копиуемого массива</param>
        /// <returns></returns>
        private static T[] SubArray<T>(this T[] array, int offset, int length)
        {
            var result = new T[length];
            Array.Copy(array, offset, result, 0, length);
            return result;
        }

        /// <summary>
        /// Вычисляет контрольную сумму токена
        /// </summary>
        /// <param name="bytes">Токен</param>
        /// <returns>Контрольная сумма</returns>
        private static long CalculateCheckSum(byte[] bytes)
        {
            long result = 0;
            foreach (var item in bytes)
                result = (long.MaxValue - item > result) ? result + item : result - item;
            return result;
        }

        /// <summary>
        /// Генерирует токен аутентификации пользователя
        /// </summary>
        /// <param name="session">Сессия пользователя</param>
        /// <returns>Токен аутентификации</returns>
        public static string GetToken(UserSession session)
        {
            if (session.IsAuthenticaed && !session.IsClosed && session.UserId > 0)
            {
                var tokenBytes = new byte[TokenLength - 8];
                var GenerationTime = DateTime.UtcNow;

                #region Generate random value
                var rand = new Random();
                long min = ((long)Int32.MaxValue) * 2;
                long max = Int64.MaxValue;
                long RandomLongValue = rand.Next((Int32)(min >> 32), (Int32)(max >> 32));
                RandomLongValue = (RandomLongValue << 32);
                RandomLongValue = RandomLongValue | (uint)rand.Next((Int32)min, (Int32)max);
                #endregion
                #region Generate reserved 24 bytes array
                var reserved = new byte[] {
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                };
                #endregion
                #region Generate base 64 bytes token
                using (var tokenStream = new MemoryStream(TokenLength))
                {
                    tokenStream.Write(BitConverter.GetBytes(session.UserId), 0, 8);
                    tokenStream.Write(BitConverter.GetBytes(GenerationTime.ToBinary()), 0, 8);
                    tokenStream.Write(BitConverter.GetBytes((GenerationTime + ValidTime).ToBinary()), 0, 8);
                    tokenStream.Write(reserved, 0, reserved.Length);
                    tokenStream.Write(BitConverter.GetBytes(RandomLongValue), 0, 8);
                    tokenStream.Seek(0, SeekOrigin.Begin);
                    tokenStream.Read(tokenBytes, 0, TokenLength - 8);
                    tokenStream.Write(BitConverter.GetBytes(CalculateCheckSum(tokenBytes)), 0, 8);
                    tokenStream.Seek(0, SeekOrigin.Begin);
                    tokenBytes = new byte[tokenStream.Length];
                    tokenStream.Read(tokenBytes, 0, tokenBytes.Length);
                }
                #endregion
                #region Compress generated token
                using (var Token = new MemoryStream())
                using (var compressedToken = new DeflateStream(Token, CompressionMode.Compress))
                {
                    compressedToken.Write(tokenBytes, 0, tokenBytes.Length);
                    compressedToken.Flush();
                    compressedToken.Close();
                    tokenBytes = Token.ToArray();
                }
                #endregion
                return new RijndaelCryptoProvider().Encrypt(tokenBytes);
            }
            throw new UserNotAthenticatedException();
        }

        /// <summary>
        /// Валидирует токен
        /// </summary>
        /// <param name="Token">Токен аутентификации</param>
        /// <returns>Уникальный идентификатор пользователя</returns>
        public static long ValidateToken(string Token)
        {
            long UserId = -1;
            DateTime GeneratedDate = DateTime.UtcNow;
            DateTime ExpireDate = DateTime.UtcNow;
            var buffer = new byte[8];
            try
            {
                var tokenBytes = new RijndaelCryptoProvider().Decrypt(Token);
                #region Decompress token
                using (var msToken = new MemoryStream(tokenBytes))
                using (var decompressedToken = new DeflateStream(msToken, CompressionMode.Decompress))
                {
                    tokenBytes = new byte[TokenLength];
                    decompressedToken.Read(tokenBytes, 0, tokenBytes.Length);
                }
                #endregion
                #region Parse token
                UserId = BitConverter.ToInt64(tokenBytes, 0);
                GeneratedDate = DateTime.FromBinary(BitConverter.ToInt64(tokenBytes, 8));
                ExpireDate = DateTime.FromBinary(BitConverter.ToInt64(tokenBytes, 16));
                #endregion
                #region Validate token
                if (UserId <= 0)
                    throw new InvalidTokenException("Указанный пользователь не существует.");
                if (BitConverter.ToInt64(tokenBytes, TokenLength - 8) != CalculateCheckSum(tokenBytes.SubArray(0, TokenLength - 8))
                    && BitConverter.ToInt64(tokenBytes, TokenLength - 8) <= 0)
                    throw new InvalidTokenException("Не верная контрольная сумма токена.");
                if ((GeneratedDate - ExpireDate) > ValidTime)
                    throw new InvalidTokenException("Время валидности токена больше допустимого.");
                if (ExpireDate < DateTime.UtcNow)
                    throw new InvalidTokenException("Срок действия токена истек.");
                #endregion
                return UserId;
            }
            catch (Exception ex) { throw new InvalidTokenException("В процессе разбора токена возникло исключение.", ex); }
            throw new InvalidTokenException();
        }
    }
}

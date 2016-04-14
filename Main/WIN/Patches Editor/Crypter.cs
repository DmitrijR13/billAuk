using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PathcesEditor2014
{
    /// <summary>
    /// Класс для шифрования/дешифрования строк
    /// </summary>
    public static class StringCrypter
    {
        /// <summary>
        /// Пароль по умолчанию
        /// </summary>
        public const string pass = "U9A=4{VSW4k~5G_Lt_]EDev)U\"k(GNm}e9qL4).AwIoewE:9],";

        /// <summary>
        /// Эта константа используется как соль для вызова функции PasswordDeriveBytes
        /// Его размер в байтах должены быть = длина ключа (keysize) / 8. По умолчанию keysize = 256, так что его размер = 32 байт.
        /// Использование 16 символьных строк дает 32 байта при преобразовании в массив байтов.
        /// </summary>
        private const string initVector = "jd854jf8ewj568fj";

        /// <summary>
        /// Длина ключа
        /// </summary>
        private const int keysize = 256;

        /// <summary>
        /// Шифрование
        /// </summary>
        /// <param name="plainText">Текст для шифрования</param>
        /// <returns>Возвращает зашифрованную строку</returns>
        public static string Encrypt(string plainText)
        {
            return Encrypt(plainText, pass);
        }

        /// <summary>
        /// Шифрование
        /// </summary>
        /// <param name="plainText">Текст для шифрования</param>
        /// <param name="passPhrase">Пароль</param>
        /// <returns>Возвращает зашифрованную строку</returns>
        public static string Encrypt(string plainText, string passPhrase)
        {
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
            byte[] keyBytes = password.GetBytes(keysize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();
            byte[] cipherTextBytes = memoryStream.ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            return Convert.ToBase64String(cipherTextBytes);
        }

        /// <summary>
        /// Дешифрование
        /// </summary>
        /// <param name="cipherText">Текст для дешифрования</param>
        /// <returns>Возвращает разшифрованную строку</returns>
        public static string Decrypt(string cipherText)
        {
            return Decrypt(cipherText, pass);
        }

        /// <summary>
        /// Дешифрование
        /// </summary>
        /// <param name="cipherText">Текст для дешифрования</param>
        /// <param name="passPhrase">Пароль</param>
        /// <returns>Возвращает разшифрованную строку</returns>
        public static string Decrypt(string cipherText, string passPhrase)
        {
            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
            byte[] keyBytes = password.GetBytes(keysize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
            MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
            CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            byte[] plainTextBytes = new byte[cipherTextBytes.Length];
            int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
            memoryStream.Close();
            cryptoStream.Close();
            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
        }
    }
}

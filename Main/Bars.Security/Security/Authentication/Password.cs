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
    /// Пароль учетной записи пользователя
    /// </summary>
    public class Password
    {
        /// <summary>
        /// Событие, возникающее при изменении значения пароля
        /// </summary>
        public event EventHandler onPasswordChanged;

        /// <summary>
        /// Возвращает константу для пустого пароля.
        /// </summary>
        public static Password Empty { get { return new Password(null); } }

        /// <summary>
        /// Проверять на совпадение с последними N паролями
        /// </summary>
        public static uint ValidateLastPasswords { get { return 3; } }

        /// <summary>
        /// Провайдер криптографии
        /// </summary>
        public static ICryptoProvider CryptoProvider { get { return new Md5CryptoProvider(); } }

        /// <summary>
        /// Хэш пароля пользователя
        /// </summary>
        private string _password = null;

        /// <summary>
        /// Пароль учетной записи пользователя
        /// </summary>
        /// <param name="InitializeHash">Хэш пароля пользователя</param>
        public Password(string InitializeHash = null)
        {
            _password = InitializeHash;
        }

        /// <summary>
        /// Оператор создания объекта
        /// </summary>
        /// <param name="PasswordToken"></param>
        /// <returns></returns>
        public static implicit operator Password(string PasswordToken)
        {
            return new Password(PasswordToken);
        }

        /// <summary>
        /// Возвращает хэши старых паролей и даты их назначения
        /// </summary>
        /// <returns>Хэши старых паролей и даты их назначения</returns>
        private IEnumerable<KeyValuePair<DateTime, string>> OldPasswordsHashs()
        {
            var rijndael = new RijndaelCryptoProvider();
            if (string.IsNullOrWhiteSpace(_password)) yield break;
            var comperssedBytes = rijndael.Decrypt(_password);
            using (var msPassword = new MemoryStream())
            {
                using (var msPasswd = new MemoryStream(comperssedBytes))
                using (var decompressedPasswd = new DeflateStream(msPasswd, CompressionMode.Decompress))
                {
                    byte[] cachedBytes;
                    int readed;
                    do
                    {
                        cachedBytes = new byte[32];
                        readed = decompressedPasswd.Read(cachedBytes, 0, cachedBytes.Length);
                        msPassword.Write(cachedBytes, 0, readed);
                    } while (readed == cachedBytes.Length);
                }
                msPassword.Seek(0, SeekOrigin.Begin);
                var cache = new byte[2];
                msPassword.Read(cache, 0, cache.Length);
                var PasswordsCount = BitConverter.ToInt16(cache, 0);
                for (var i = 0; i < PasswordsCount; i++)
                {
                    cache = new byte[4];
                    msPassword.Read(cache, 0, cache.Length);
                    var PasswordLength = BitConverter.ToInt16(cache, 0);
                    cache = new byte[8];
                    msPassword.Read(cache, 0, cache.Length);
                    var CreateDate = DateTime.FromBinary(BitConverter.ToInt64(cache, 0));
                    cache = new byte[PasswordLength];
                    msPassword.Read(cache, 0, cache.Length);
                    yield return new KeyValuePair<DateTime, string>(CreateDate, Encoding.Default.GetString(cache));
                }
            }
        }

        /// <summary>
        /// Создает хэш паролей
        /// </summary>
        /// <param name="passwords">Список паролей для хранения</param>
        /// <returns>Хэш паролей</returns>
        private string BuildHashString(IEnumerable<KeyValuePair<DateTime, string>> passwords)
        {
            byte[] PasswordBytes;
            using (var msPassword = new MemoryStream())
            {
                var cache = BitConverter.GetBytes((Int16)passwords.Count());
                msPassword.Write(cache, 0, cache.Length);
                foreach (var password in passwords)
                {
                    cache = BitConverter.GetBytes(password.Value.Length);
                    msPassword.Write(cache, 0, cache.Length);
                    cache = BitConverter.GetBytes(password.Key.ToBinary());
                    msPassword.Write(cache, 0, cache.Length);
                    cache = Encoding.Default.GetBytes(password.Value);
                    msPassword.Write(cache, 0, cache.Length);
                }
                msPassword.Seek(0, SeekOrigin.Begin);
                PasswordBytes = new byte[msPassword.Length];
                msPassword.Read(PasswordBytes, 0, PasswordBytes.Length);
            }
            using (var Token = new MemoryStream())
            using (var compressedToken = new DeflateStream(Token, CompressionMode.Compress))
            {
                compressedToken.Write(PasswordBytes, 0, PasswordBytes.Length);
                compressedToken.Flush();
                compressedToken.Close();
                PasswordBytes = Token.ToArray();
            }
            return new RijndaelCryptoProvider().Encrypt(PasswordBytes);
        }

        /// <summary>
        /// Изменяет пароль пользователя
        /// </summary>
        /// <param name="OldPassword">Старый пароль</param>
        /// <param name="NewPassword">Новый пароль</param>
        /// <param name="ConfirmPassword">Подтверждение пароля</param>
        public void ChangePassword(string OldPassword, string NewPassword, string ConfirmPassword = null)
        {
            var exc = new List<Exception>();
            if (string.IsNullOrWhiteSpace(NewPassword)) exc.Add(new FormatException("Пароль не может быть пустым"));
            if (NewPassword != (ConfirmPassword ?? (ConfirmPassword = NewPassword))) exc.Add(new Exception("Пароль и его подтверждение не совпадают"));

            var passwords = new List<KeyValuePair<DateTime, string>>(OldPasswordsHashs());
            try
            {
                if (passwords.Count() > 0 &&
                    passwords.Last().Value != CryptoProvider.ComputeHash(OldPassword))
                    exc.Add(new InvalidDataException("Пароль не прошел проверку подленности"));
                else if (passwords.Where(x => x.Value == CryptoProvider.ComputeHash(NewPassword)).Count() == 0)
                {
                    while (passwords.Count() >= ValidateLastPasswords)
                        passwords.Remove(passwords.First());
                }
                else exc.Add(new InvalidDataException("Этот пароль уже использовался ранее."));

                if (exc.Count == 0)
                {
                    passwords.Add(new KeyValuePair<DateTime, string>(DateTime.UtcNow, CryptoProvider.ComputeHash(NewPassword)));
                    _password = BuildHashString(passwords);
                    if (onPasswordChanged != null)
                        onPasswordChanged(this, new EventArgs());
                }
            }
            catch (Exception ex) { exc.Add(ex); }
            if (exc.Count > 0) throw new AggregateException("В процессе обработки были обнаружены ошибки.", exc);
        }

        /// <summary>
        /// Валидирует пароль
        /// </summary>
        /// <param name="password">Пароль, который проверяется</param>
        /// <returns></returns>
        public bool IsValid(string password)
        {
            if (OldPasswordsHashs().Count() < 1) return false;
            var passwd = OldPasswordsHashs().Last();
            return (passwd.Value == CryptoProvider.ComputeHash(password));
        }

        /// <summary>
        /// Возвращает хэшированный пароль
        /// </summary>
        /// <returns>Возвращает хэшированный пароль</returns>
        public override string ToString()
        {
            return _password == null ? string.Empty : _password;
        }
    }
}

using System;
using System.Text;
using System.Security.Cryptography;

namespace webroles
{
    class PasswordHashCode
    {

     public  static string createHash(string source)
        {
            string hash;
            using (MD5 md5Hash = MD5.Create())
            {
                 hash = GetMd5Hash(md5Hash, source);
            }
            return hash;
        }
        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));
            return sBuilder.ToString();
        }

        static bool VerifyMd5Hash(string inputPassword, string hashPasswordFromPostgre)
        {
            string hashOfInput;
            // Вычисление хэш введенного пароля
            using (MD5 md5Hash = MD5.Create())
             hashOfInput = GetMd5Hash(md5Hash, inputPassword);
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            if (0 == comparer.Compare(hashOfInput, hashPasswordFromPostgre))
                return true;
                return false;
        }
    }
}


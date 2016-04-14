using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

namespace ReportServer
{
    /// <summary>
    /// класс считывания конфиг файла
    /// </summary>
    public class ConfigLoad
    {
        private static string RegexConfig = "(?i)(?<=\\s*\\<[\\w\\s]+=\\\")(?<pref>[\\w]+)(\\\"[\\s\\w]+=\\\")(?<value>[-=+\\w]+)(?=\\\" />)";

        /// <summary>
        /// возвращает массив значений конфигурационного файла
        /// </summary>
        /// <param name="PathToConfig"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetValuesFromConfig(string PathToConfig)
        {
            Dictionary<string, string> Result = new Dictionary<string, string>();
            StreamReader sr = new StreamReader(PathToConfig);
            string str;
            Regex regex = new Regex(RegexConfig);
            while ((str = sr.ReadLine()) != null)
            {
                Match match = regex.Match(str);
                if (match.Success)
                {
                    string key = match.Groups["pref"].ToString();
                    string value = Encryptor.Decrypt(match.Groups["value"].ToString(), null);
                    Result.Add(key, value);
                }
            }
            sr.Close();
            return Result;
        }
    }
}

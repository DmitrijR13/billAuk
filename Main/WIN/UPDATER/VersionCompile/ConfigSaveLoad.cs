using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;

namespace VersionCompile
{
    class ConfigSaveLoad
    {
        private static string RegexConfig = "(?i)(?<=\\s*\\<[\\w\\s]+=\\\")(?<pref>[\\w]+)(\\\"[\\s\\w]+=\\\")(?<value>[-=+\\w]+)(?=\\\" />)";

        public static ArrayList GetValuesFromConfig(string PathToConfig)
        {
            ArrayList Result = new ArrayList();
            string[] masstr = new string[2];
            StreamReader sr = new StreamReader(PathToConfig);
            string str;
            Regex regex = new Regex(RegexConfig);
            while ((str = sr.ReadLine()) != null)
            {
                Match match = regex.Match(str);
                if (match.Success)
                {
                    masstr[0] = match.Groups["pref"].ToString();
                    masstr[1] = Encryptor.Decrypt(match.Groups["value"].ToString(), null);
                    Result.Add(new string[2] { masstr[0], masstr[1] });
                }
            }
            sr.Close();
            return Result;
        }

        public static void SaveValuesToConfig(string PathToConfig, ArrayList Values, int Type)
        {
            FileAttributes FA = FileAttributes.Normal;
            if (File.Exists(PathToConfig))
            {
                FA = File.GetAttributes(PathToConfig);
                File.SetAttributes(PathToConfig, FileAttributes.Normal);
            }
            string BeginString = "", AddString = "", ValueString = "", EndString = "";
            switch (Type)
            {
                case 0:
                    {
                        BeginString = "<appSettings>";
                        AddString = "  <add key=\"";
                        ValueString = "\" value=\"";
                        EndString = "</appSettings>";
                        break;
                    }
                case 1:
                    {
                        BeginString = "<connectionStrings>";
                        AddString = "  <add name=\"";
                        ValueString = "\" connectionString=\"";
                        EndString = "</connectionStrings>";
                        break;
                    }
            }

            StreamWriter sw = new StreamWriter(PathToConfig);
            sw.WriteLine(BeginString);
            foreach (string[] masstr in Values)
            {
                sw.WriteLine(AddString + masstr[0] + ValueString + Encryptor.Encrypt(masstr[1], null) + "\" />");
            }
            sw.WriteLine(EndString);
            sw.Close();
            File.SetAttributes(PathToConfig, FA);
        }
    }
}

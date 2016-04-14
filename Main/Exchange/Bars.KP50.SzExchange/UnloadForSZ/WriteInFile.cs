using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bars.KP50.SzExchange.UnloadForSZ
{
    class WriteInFile
    {
        public void Filing(string str)
        {
            StreamWriter writer = new StreamWriter("C:\\tmp\\test.txt", true, System.Text.Encoding.GetEncoding(866));
            writer.WriteLine(str);
            writer.Close(); 
        }


        public void Filing(string str, string path)
        {
            //проверяем - есть ли директория, если ее нет, то создаем
            if(!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
        
            //необходимо выгружать именно в кодировке 866
            StreamWriter writer = new StreamWriter(path, true, System.Text.Encoding.GetEncoding(866));
            writer.WriteLine(str);
            writer.Close();
        }


        public void WriteFile(string str)
        {
            StreamWriter writer = new StreamWriter("C:\\tmp_passport\\text.txt", true, System.Text.Encoding.GetEncoding(866));
            writer.WriteLine(str);
            writer.Close();
        }


    }
}

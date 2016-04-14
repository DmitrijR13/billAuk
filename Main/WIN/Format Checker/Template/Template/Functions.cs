using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1
{
    class Functions
    {
        public Func<string> FLoad1;
        public Func<string> FLoad2;

        public Functions()
        {
            FLoad1 = Load1;
            FLoad2 = Load2;
        }

        private string Load1()
        {
            Console.WriteLine("Load1");
            return "1";
        }
        private string Load2()
        {
            Console.WriteLine("Load2");
            return "2";
        }
    }
}

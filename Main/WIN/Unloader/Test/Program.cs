using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unloader;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = UnloadInstanse.Instance.GetAllFormats();
            var db = UnloadInstanse.Instance.GetDatabases();
            var sch = UnloadInstanse.Instance.GetShemas("kaprem");
            var poin = UnloadInstanse.Instance.GetPoints("kaprem","nftul");
            Console.Read();
        }
    }
}

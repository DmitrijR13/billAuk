using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.File.Loader
{
    class RowClass
    {
        public string num { get; set; }
        public string fileName { get; set; }
        public string dateFile { get; set; }
        public string logLink { get; set; }
        public string savedName { get; set; }
        public int nzp_file { get; set; }

        public RowClass()
        {
            num = "";
            fileName = "";
            dateFile = "";
            logLink = "";
            savedName = "";
            nzp_file = 0;
        }
    }
}

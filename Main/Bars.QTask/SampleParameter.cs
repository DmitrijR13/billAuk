using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.QTask
{
    public class SampleParameter
    {
        public DateTime Date { get; set; }

        public SampleParameter()
        {
            Date = DateTime.UtcNow;
        }
    }
}

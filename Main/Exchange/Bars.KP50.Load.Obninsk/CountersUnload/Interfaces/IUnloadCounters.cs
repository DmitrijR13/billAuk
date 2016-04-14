using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Bars.KP50.Load.Obninsk.Progress.Interfaces;

namespace Bars.KP50.Load.Obninsk.CountersUnload.Interfaces
{
    public interface IUnloadCounters:IDisposable
    {
        void Init(IBaseLoadProtokol protokol, IDbConnection connection, StreamWriter writer, int nzp_exc);
        void Unload(List<int> list_nzp_wp );
        String Name { get;}
        String Description { get; }
    }

}

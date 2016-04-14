using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bars.KP50.Load.Obninsk.Progress;
using Castle.MicroKernel.ModelBuilder.Descriptors;

namespace Bars.KP50.Load.Obninsk.Progress.Interfaces
{
    public interface IProgressWork<T> where T: EventArgs
    {
        event EventHandler<T> RaiseProgressEvent;
        void Init(int totalCount);
        void IncrementProgress(int countInserted);
    }
}

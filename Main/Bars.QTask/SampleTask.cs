using Bars.QTask.Tasks;
using System;
using System.Threading;

namespace Bars.QTask
{
    [TaskExport(TaskType.SampleTask)]
    public class SampleTask : ExecutableTask
    {
        public override Type ContainerType { get { return typeof(SampleParameter); } }

        public override void Execute(object container, ThreadValidationToken token)
        {
            for (int i = 0; i < 100; i++)
            {
                token.Validate();
                Progress += 0.01m;
                Thread.Sleep(500);
            }
        }
    }
}

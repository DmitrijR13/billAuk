using System.IO;
using Bars.Worker;

namespace SimpleJob
{
    public class SimpleReportJob : IJob
    {
        public State GetState()
        {
            return new State();
        }

        public void Run(ExecutionArguments @params)
        {

            File.WriteAllText(@"C:\test_report.txt", @params.TaskName);
        }
    }
}

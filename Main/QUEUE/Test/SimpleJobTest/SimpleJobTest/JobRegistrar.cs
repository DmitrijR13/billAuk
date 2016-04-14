using Bars.Worker;
using Castle.Windsor;

namespace SimpleJob
{
    public class JobRegistrar: IJobRegistrar
    {
        public void Register(IWindsorContainer container)
        {
            container.RegisterJob<SimpleReportJob>("SimpleReport");
        }
    }
}

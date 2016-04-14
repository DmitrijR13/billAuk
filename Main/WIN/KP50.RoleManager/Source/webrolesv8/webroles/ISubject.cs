using System.Collections.Generic;

namespace webroles
{
    interface ISubject
    {
        void AddObservers(List<IObserver> obsrvs);
        void NotifyObservers();
    }
}

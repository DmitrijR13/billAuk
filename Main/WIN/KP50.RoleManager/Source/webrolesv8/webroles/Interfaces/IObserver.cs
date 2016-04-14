using System.Data;
namespace webroles
{
    interface IObserver
    {
        string AdditionalContotionString { get; }
        bool AdditionalContition{get;}
        void update(DataTable dataTable);
        void update(bool isZeroNeeded=false);
    }
}

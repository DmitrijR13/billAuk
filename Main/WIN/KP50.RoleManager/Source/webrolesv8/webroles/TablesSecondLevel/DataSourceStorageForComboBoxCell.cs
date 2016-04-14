using System.Data;

namespace webroles.TablesSecondLevel
{
    class DataSourceStorageForComboBoxCell
    {
     public   DataTable dt {get;set;}
     public   int num { get; set; }
       
       public  DataSourceStorageForComboBoxCell (DataTable dt, int num)
        {
            this.dt = dt;
            this.num = num;
        }

 
    }
}

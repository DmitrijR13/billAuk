using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_HttpTest
    {
        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        [JSONPBehavior(callback = "method")]
        Location JSONData_1();
    }

    public class Location
    {
        public string Latitude;
        public string Longitude;

        public Location()
        {
            this.Latitude = "-";
            this.Longitude = "-";
        }
        public Location(string lat, string long1)
        {
            this.Latitude = lat;
            this.Longitude = long1;
        }
    }

}

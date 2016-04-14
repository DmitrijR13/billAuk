using System.Configuration;

namespace Loader
{
    public static class ConfigurateReader
    {
        public static string[] GetConfig()
        {
            return new[] { 
                ConfigurationManager.AppSettings["connString"], 
                ConfigurationManager.AppSettings["psql"], 
                ConfigurationManager.AppSettings["server"],
                ConfigurationManager.AppSettings["database"],
                ConfigurationManager.AppSettings["port"],
                ConfigurationManager.AppSettings["user"],
                ConfigurationManager.AppSettings["password"] };
        }
    }
}

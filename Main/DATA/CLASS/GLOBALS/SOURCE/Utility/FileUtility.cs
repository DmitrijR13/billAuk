using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.AccessControl;
using System.Security.Principal;
using System.IO;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Utility
{
    public static class FileUtility
    {
        public static void CreateDirectory(string directory)
        {
                System.IO.Directory.CreateDirectory(directory);
                /*DirectorySecurity dSecurity = Directory.GetAccessControl(directory);
                dSecurity.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().Name, FileSystemRights.FullControl, AccessControlType.Allow));
                Directory.SetAccessControl(directory, dSecurity);*/
        }

        public static string GetFileName(string directory, string filename)
        {
            int i = 0;
            DirectoryInfo di = new DirectoryInfo(directory);
            try
            {
                FileInfo[] files = di.GetFiles();


                foreach (FileInfo fi in files)
                {
                    if (fi.Name.Contains(filename))
                    {
                        i++;
                    }
                }

                if (i == 0)
                {
                    return filename;
                }
                else
                {
                    return filename + "_" + i;
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message + " Но все равно будет работать!", MonitorLog.typelog.Warn, true);
                return filename + "_" + DateTime.Now.Ticks;
            }
        }
    }
}

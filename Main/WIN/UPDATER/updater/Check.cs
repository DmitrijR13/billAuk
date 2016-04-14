using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using System.ServiceModel;
using System.Drawing;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;

namespace updater
{
    public class Check
    {
        #region Проверка подключений к районам

        static public bool CheckConnect(Info rajon)
        {
            bool checker = false;
            try
            {
                cli_Patch cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                checker = cli.CheckConn() == 1;
            }
            catch
            {
                checker = false;
            }
            rajon.connect = checker;
            return checker;
        }

        //static public bool CheckConnectImg(string rajon_name, string connect_str) 
        //{
        //    bool checker;
        //    try
        //    {
        //        //cli_Patch cli = new cli_Patch(connect_str, "Administrator", "rubin");
        //        cli_Patch cli = new cli_Patch(connect_str, "Administrator", "rubin");
        //        checker = cli.CheckConn();
        //    }
        //    catch
        //    {
        //        checker = false;
        //    }
        //    return checker;
        //}


        #endregion
    }
}

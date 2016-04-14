using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace updater
{
    public class Info
    {
        public string rajon_number;
        public string rajon_name;
        public string rajon_ip;
        public string rajon_login;
        public string rajon_password;
        public string rajon_basename;
        public string rajon_webpath;
        public string update_status;
        public string update_type;
        public string update_version;
        public string update_date;
        public string update_report;
        public string now_status;
        public string php_ready;
        public bool connect;
        public int rowindex;

        public Info()
        {
            rajon_number = "NO DATA";
            rajon_name = "NO DATA";
            rajon_ip = "NO DATA";
            rajon_login = "NO DATA";
            rajon_password = "NO DATA";
            rajon_basename = "NO DATA";
            rajon_webpath = "NO DATA";
            update_type = "NO DATA";
            update_status = "NO DATA";
            update_version = "NO DATA";
            update_date = "NO DATA";
            update_report = "NO DATA";
            now_status = "NO DATA";
            php_ready = "NO DATA";
            connect = false;
            rowindex = -1;
        }
    }

    public class PHPInfo
    {
        public int rajon_number;
        public string rajon_name;
        public string dat_when;
        public string dat_pgu;
        public string cnt_access;
        public string cnt_ls;
        public string cnt_device;
        public string cnt_faktura;
        public string cnt_pay;
        public string cnt_access_day;
        public string cnt_ls_day;
        public string cnt_device_day;
        public string cnt_faktura_day;
        public string cnt_pay_day;

        public PHPInfo()
        {
            rajon_number = 0;
            rajon_name = "NO DATA";
            dat_when = "NO DATA";
            dat_pgu = "NO DATA";
            cnt_access = "NO DATA";
            cnt_ls = "NO DATA";
            cnt_device = "NO DATA";
            cnt_faktura = "NO DATA";
            cnt_pay = "NO DATA";
            cnt_access_day = "NO DATA";
            cnt_ls_day = "NO DATA";
            cnt_device_day = "NO DATA";
            cnt_faktura_day = "NO DATA";
            cnt_pay_day = "NO DATA";
        }
    }
}

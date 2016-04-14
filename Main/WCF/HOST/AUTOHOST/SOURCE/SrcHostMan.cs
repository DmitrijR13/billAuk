using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;

using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Client;

using STCLINE.KP50.Server;
using System.Text.RegularExpressions;

namespace STCLINE.KP50.HostMan
{
    using System.Runtime.InteropServices.ComTypes;

    /*
    public class ConfigKey : INotifyPropertyChanged
    {
        private string _key;
        private string _val;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        public string key 
        {
            get { return _key; }
            set { _key = value; }
        }
        public string val
        {
            get { return _val; }
            set
            {
                if (value != this._val)
                {
                    this._val = value;
                    NotifyPropertyChanged("val");
                }
            }
        }
    }
    */
    public class ChangeVal
    {
        protected bool _changed;
        public bool changed
        {
            get { return _changed; }
        }
    }
    public class ConfigKey : ChangeVal
    {
        private string _key;
        private string _val;

        public string key
        {
            get { return _key; }
            set
            {
                _key = value;
                _changed = false;
            }
        }
        public string val
        {
            get { return _val; }
            set
            {
                if (value != this._val)
                {
                    this._val = value;
                    _changed = true;
                }
            }
        }
    }
    public class PwdKey : ChangeVal
    {
        private int _nzp_user;
        private string _login;
        private string _pwd;
        private string _email;
        private string _uname;

        public int nzp_user
        {
            get { return _nzp_user; }
            set
            {
                _nzp_user = value;
                _changed = false;
            }
        }
        public string login
        {
            get { return _login; }
            set
            {
                if (value != this._login)
                {
                    this._login = value;
                    _changed = true;
                }
            }
        }
        public string pwd
        {
            get { return _pwd; }
            set
            {
                if (value != this._pwd)
                {
                    this._pwd = value;
                    _changed = true;
                }
            }
        }
        public string uname
        {
            get { return _uname; }
            set
            {
                if (value != this._uname)
                {
                    this._uname = value;
                    _changed = true;
                }
            }
        }
        public string email
        {
            get { return _email; }
            set
            {
                if (value != this._email)
                {
                    this._email = value;
                    _changed = true;
                }
            }
        }
    }

    public class SrcHostMan
    {
        /// <summary>
        /// Для получения параметров конфига с помощью рег. выражения
        /// </summary>
        private static string RegexConfig = "(?i)(?<=\\s*\\<[\\w\\s]+=\\\")(?<key>[\\w]+)(\\\"[\\s\\w]+=\\\")(?<value>[-=+\\w]+)(?=\\\" />)";
        //------------------------------------------------------------
        //
        // Сохранение изменений
        //
        //------------------------------------------------------------
        public string Save(List<PwdKey> dGrid)
        {
            List<User> users = new List<User>();

            foreach (PwdKey zap in dGrid)
            {
                if (zap.changed)
                {
                    User user = new User();
                    user.login = zap.login;
                    user.email = zap.email;
                    user.uname = zap.uname;
                    user.nzp_user = zap.nzp_user;
                    user.pwd = zap.pwd;

                    users.Add(user);
                }
            }
            if (users.Count < 1) return "Изменения не обнаружены";

            MessageBoxButtons mb = MessageBoxButtons.YesNo;
            DialogResult rezMsgDialog = new DialogResult();
            rezMsgDialog = MessageBox.Show("Сохранить изменения?", "Изменение данных", mb, MessageBoxIcon.Question);
            if (!rezMsgDialog.Equals(DialogResult.Yes))
            {
                return "Изменения не сохранены";
            }


            Returns ret = Utils.InitReturns();
            using (var db = new DbWorkUserClient())
            {
                db.SaveWebUser(users, out ret);
            }

            if (ret.result)
            {
                return "Данные успешно сохранены";
            }
            else
            {
                return "Ошибка сохранения: " + ret.text;
            }

        }
        public string Save(List<ConfigKey> dGrid, enGrid grid, string dir)
        {
            bool b = true;
            foreach (ChangeVal zap in dGrid)
            {
                if (zap.changed)
                {
                    b = false;
                    break;
                }
            }
            if (b) return "Изменения не обнаружены";

            string filename = dir + @"\Connect.config";
            string f_head = "connectionStrings";
            string f_key = "name";
            string f_val = "connectionString";

            if (grid == enGrid.Host || grid == enGrid.Broker)
            {
                if (grid == enGrid.Host) filename = dir + @"\Host.config";
                else filename = dir + @"\Broker.config";
                f_head = "appSettings";
                f_key = "key";
                f_val = "value";
            }

            try
            {
                FileInfo fi = new FileInfo(filename);
                fi.Attributes = FileAttributes.Normal;
                fi.CopyTo(filename + ".save", true);
                fi.Delete();

                using (StreamWriter sw = new StreamWriter(filename))
                {
                    sw.WriteLine(@"<" + f_head + ">");
                    foreach (ConfigKey zap in dGrid)
                    {
                        sw.WriteLine(@"  <add " + f_key + "=\"" + zap.key + "\" " + f_val + "=\"" + Encryptor.Encrypt(zap.val, null) + "\" />");
                    }
                    sw.WriteLine(@"</" + f_head + ">");
                }
            }
            catch (Exception ex)
            {
                return "Ошибка записи в файл: " + ex.Message;
            }

            return "Данные успешно сохранены";
        }

        public string SaveToFile(List<ConfigKey> dGrid, enGrid grid, string filename)
        {
            bool b = true;
            foreach (ChangeVal zap in dGrid)
            {
                if (zap.changed)
                {
                    b = false;
                    break;
                }
            }
            if (b) return "Изменения не обнаружены";

            string f_head = "connectionStrings";
            string f_key = "name";
            string f_val = "connectionString";

            if (grid == enGrid.NewHost)
            {
                f_head = "appSettings";
                f_key = "key";
                f_val = "value";
            }

            try
            {
                FileInfo fi = new FileInfo(filename);
                fi.Attributes = FileAttributes.Normal;
                fi.CopyTo(filename + ".save", true);
                fi.Delete();

                using (StreamWriter sw = new StreamWriter(filename, false))
                {
                    sw.WriteLine(@"<" + f_head + ">");
                    foreach (ConfigKey zap in dGrid)
                    {
                        sw.WriteLine(@"  <add " + f_key + "=\"" + zap.key + "\" " + f_val + "=\"" + Encryptor.Encrypt(zap.val, null) + "\" />");
                    }
                    sw.WriteLine(@"</" + f_head + ">");
                }
            }
            catch (Exception ex)
            {
                return "Ошибка записи в файл: " + ex.Message;
            }

            return "Данные успешно сохранены";
        }

        //------------------------------------------------------------
        //
        // Работа с Pwd
        //
        //------------------------------------------------------------
        public void GetPwd(List<PwdKey> dGrid, int pwdSort, out Returns ret)
        {
            ret = Utils.InitReturns();
            dGrid.Clear();

            User finder = new User();
            finder.nzp_user = 1;
            finder.rows = 10000;
            finder.sortby = pwdSort; // Constants.sortby_login;
            DbAdmin db = new DbAdmin();
            ReturnsObjectType<List<User>> list = db.GetUsers(finder);
            db.Close();

            if (list == null)
            {
                ret.result = false;
                ret.text = "Ошибка в функции GetPwd";
                return;
            }
            if (!list.result)
            {
                ret = list.GetReturns();
                return;
            }

            if (list.returnsData == null)
            {
                ret.result = false;
                ret.text = "Список пуст";
                return;
            }

            foreach (User zap in list.returnsData)
            {
                PwdKey pwd = new PwdKey();
                pwd.login = zap.login;
                pwd.email = zap.email;
                pwd.uname = zap.uname;

                //конвертировать пароль
                pwd.pwd = zap.pwd.Replace(zap.nzpuser + "-", "");

                pwd.nzp_user = zap.nzpuser;

                dGrid.Add(pwd);
            }

        }
        //------------------------------------------------------------
        //
        // Работа с config
        //
        //------------------------------------------------------------
        private string TryDecrypt(string s)
        {
            try
            {
                return Encryptor.Decrypt(s, null);
            }
            catch
            {
                return "";
            }
        }
        public void HostConfig(string fn, List<ConfigKey> dGrid, out Returns ret)
        {
            ret = Utils.InitReturns();
            dGrid.Clear();
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(fn);

                string sectionName = "appSettings";
                AppSettingsSection appSettings = (AppSettingsSection)config.GetSection(sectionName);

                //tb_Info.AppendText("<appSettings> \r\n");
                if (appSettings.Settings.Count != 0)
                {
                    foreach (string key in appSettings.Settings.AllKeys)
                    {
                        string value = TryDecrypt(appSettings.Settings[key].Value);
                        if (!string.IsNullOrEmpty(value))
                        {
                            //tb_Info.AppendText("<add key='" + key + "' value='" + Encryptor.Decrypt(value, null) + "'/> \r\n" );
                            //tb_Info.AppendText(key + " | " + value + " \r\n");
                            ConfigKey conf = new ConfigKey();
                            conf.val = value;
                            conf.key = key;
                            dGrid.Add(conf);
                        }
                        //tb_Info.AppendText(value);
                    }
                }
                else
                {
                    ret.text = "Строки не определены!";
                    ret.result = false;
                }
            }
            catch (ConfigurationErrorsException e)
            {
                ret.text = e.Message;
                ret.result = false;
            }
        }
        public void WebConfig(string fn, List<ConfigKey> dGrid, out Returns ret)
        {
            ret = Utils.InitReturns();
            dGrid.Clear();
            try
            {
                FileStream f = new FileStream(fn, FileMode.Create, FileAccess.Write);
                f.Close();

                Configuration config = ConfigurationManager.OpenExeConfiguration(fn);

                ConnectionStringsSection sect = config.ConnectionStrings;
                ConnectionStringSettingsCollection connections = sect.ConnectionStrings;

                if (connections.Count != 0)
                {
                    foreach (ConnectionStringSettings connection in connections)
                    {
                        string name = connection.Name;
                        string connectionString = TryDecrypt(connection.ConnectionString);

                        if (!string.IsNullOrEmpty(connectionString))
                        {
                            //tb_Info.AppendText(name + " | " + connectionString + " \r\n");
                            //tb_Info.AppendText("<add key='" + key + "' value='" + TryDecrypt(value) + "'/> \r\n");
                            ConfigKey conf = new ConfigKey();
                            conf.val = connectionString;
                            conf.key = name;
                            dGrid.Add(conf);
                        }
                    }
                }
                else
                {
                    ret.text = "Строки не определены!";
                    ret.result = false;
                }
            }
            catch (ConfigurationErrorsException e)
            {
                ret.text = e.Message;
                ret.result = false;
            }
        }

        //------------------------------------------------------------
        //
        // Test
        //
        //------------------------------------------------------------
        public string SaldoFon()
        {
            Finder finder = new Finder();
            finder.nzp_user = 1;
            Returns ret = Utils.InitReturns();

            DbCalc db = new DbCalc();
            //db.SaldoFon(2011, 1, out ret);
            db.CalcFonProc(2);
            db.Close();


            if (ret.result)
                return "Операция проведена успешно!";
            else
                return "Ошибка: " + ret.text;
        }
        public string FirstRunApp()
        {
            Finder finder = new Finder();
            finder.nzp_user = 1;
            Returns ret = Utils.InitReturns();

            DbAnaliz db = new DbAnaliz();
            db.LoadAdres(finder, out ret, Points.CalcMonth.year_, true);
            db.Close();

            if (!ret.result)
                return "Ошибка: " + ret.text;

            AnlSupp finder1 = new AnlSupp();
            finder1.nzp_user = 1;

            DbAnaliz db2 = new DbAnaliz();
            db2.LoadSupp(finder1, out ret, Points.CalcMonth.year_, true);
            db2.Close();

            if (!ret.result)
                return "Ошибка: " + ret.text;

            DbAdmin db3 = new DbAdmin();
            db3.FirstRunCreateUsers(finder, out ret);
            db3.Close();

            if (ret.result)
                return "Операция проведена успешно!";
            else
                return "Ошибка: " + ret.text;
        }
        public string TestDostup()
        {
            Returns ret = Utils.InitReturns();

            DbAdmin db = new DbAdmin();
            db.TestDostup(out ret);
            db.Close();

            if (!ret.result)
                return "Ошибка: " + ret.text;
            else
                return "ОК: " + ret.text + ".....";
        }

        public string TestCalcGilXX()
        {
            Returns ret = Utils.InitReturns();

            //DbCalc.CalcGilXX_Test(out ret);
            if (!ret.result)
                return "Ошибка: " + ret.text;
            else
                return "ОК: " + ret.text + ".....";
        }

        public string CalcGkuXX_Do()
        {
            Returns ret = Utils.InitReturns();

            DbCalcCharge db = new DbCalcCharge();
            db.CalcGkuXX_Run(out ret);
            db.Close();

            if (!ret.result)
                return "Ошибка: " + ret.text;
            else
                return "ОК: " + ret.text + ".....";
        }

        public string CalcRashod()
        {
            Returns ret = Utils.InitReturns();

            DbCalc db = new DbCalc();  /* false true, port */
            //db.TestCalcRashod(5368, "mnz", 2011, 8, out ret);
            db.CalcLs(134003312, "sav08", 2012, 6, 2012, 6, true, false, "", 0, out ret);
            db.Close();

            if (!ret.result)
                return "Ошибка: " + ret.text;
            else
                return "ОК: " + ret.text + ".....";
        }

        public string StartCalc(int nzp_dom, string pref/*, int calc_yy, int calc_mm, int cur_yy, int cur_mm*/, bool[] clc)
        {
            Returns ret = Utils.InitReturns();

            //int nzp_dom, string pref, int yy, int mm, out Returns ret
            DbCalc db = new DbCalc();
            db.CalcOnFon(nzp_dom, pref/*, calc_yy, calc_mm, cur_yy, cur_mm*/, false, out ret);
            db.Close();

            if (!ret.result)
                return "Ошибка: " + ret.text;
            else
                return "ОК: " + ret.text + ".....";
        }

        //------------------------------------------------------------
        //
        // Test
        //
        //------------------------------------------------------------
        public string TestAdres()
        {
            Returns ret;
            Ls kvar = new Ls();
            kvar.nzp_dom = 1;
            kvar.nzp_area = 1;
            kvar.nzp_geu = 1;
            kvar.typek = 1;
            kvar.pref = "vas";
            kvar.nzp_user = 1;
            kvar.webLogin = "system";
            kvar.webUname = "webuser";

            DbAdresHard db = new DbAdresHard();
            db.Update(kvar, out ret);
            db.Close();

            return ret.text;
        }
        public string TestIntervals()
        {
            Returns ret;
            EditInterData editData = new EditInterData();

            editData.nzp_wp = 11;
            editData.pref = "vas";
            editData.nzp_user = 1;
            editData.webLogin = "system";
            editData.webUname = "webuser";
            editData.table = "nedop_kvar";
            editData.primary = "nzp_nedop";
            editData.dat_s = "01.01.2010 13:00";
            editData.dat_po = "12.01.2010 13:00";
            editData.intvType = enIntvType.intv_Hour;

            //условие выборки данных из целевой таблицы
            editData.dopFind = new List<string>();
            //editData.dopFind.Add(" and nzp_kvar = 303080 and nzp_serv = 5 ");
            editData.dopFind.Add(" and nzp_kvar in ( select nzp_kvar from webzel@ol_bars:t1_spls ) and nzp_serv = 5 ");

            //ключевые поля
            Dictionary<string, string> keys = new Dictionary<string, string>();

            keys.Add("nzp_kvar", "1|nzp_kvar|kvar|nzp_kvar in ( select nzp_kvar from webzel@ol_bars:t1_spls )"); //ссылка на ключевую таблицу
            keys.Add("nzp_serv", "2|5");

            //keys.Add("nzp",     "1|nzp_kvar|kvar|nzp_kvar=303080"); //ссылка на ключевую таблицу
            //keys.Add("nzp_prm", "2|5");
            //keys.Add("nzp",     "1|nzp_dom|dom|nzp_dom=303080"); //ссылка на ключевую таблицу
            //keys.Add("nzp_prm", "2|5");

            editData.keys = keys;


            //вставляемые значения
            Dictionary<string, string> vals = new Dictionary<string, string>();
            vals.Add("nzp_supp", "0");
            vals.Add("nzp_kind", "1");
            vals.Add("tn", "");
            editData.vals = vals;

            //cli_EditInterData cli = new cli_EditInterData();
            //cli.Saver(editData, out ret);

            DbEditInterData db = new DbEditInterData();
            db.Saver(editData, out ret);
            db.Close();

            return ret.text;
        }
        public string TestIntervals2()
        {
            Returns ret;
            EditInterData editData = new EditInterData();

            editData.nzp_wp = 16;
            editData.pref = "zel";
            editData.nzp_user = 1;
            editData.webLogin = "system";
            editData.webUname = "webuser";
            editData.table = "tarif";
            editData.primary = "nzp_tarif";
            editData.dat_s = "01.02.2011";
            editData.dat_po = "01.01.3000";
            editData.intvType = enIntvType.intv_Day;

            //условие выборки данных из целевой таблицы
            editData.dopFind = new List<string>();
            editData.dopFind.Add(" and nzp_kvar = 2653 and nzp_serv = 6 ");
            //editData.dopFind.Add(" and nzp_kvar in ( select nzp_kvar from webzel@ol_bars:t1_spls ) and nzp_serv = 5 ");

            //ключевые поля
            Dictionary<string, string> keys = new Dictionary<string, string>();

            //keys.Add("nzp_kvar", "1|nzp_kvar|kvar|nzp_kvar in ( select nzp_kvar from webzel@ol_bars:t1_spls )"); //ссылка на ключевую таблицу
            keys.Add("nzp_kvar", "1|nzp_kvar|kvar|nzp_kvar = 2653 "); //ссылка на ключевую таблицу
            keys.Add("nzp_serv", "2|6");

            //keys.Add("nzp",     "1|nzp_kvar|kvar|nzp_kvar=303080"); //ссылка на ключевую таблицу
            //keys.Add("nzp_prm", "2|5");
            //keys.Add("nzp",     "1|nzp_dom|dom|nzp_dom=303080"); //ссылка на ключевую таблицу
            //keys.Add("nzp_prm", "2|5");

            editData.keys = keys;


            //вставляемые значения
            Dictionary<string, string> vals = new Dictionary<string, string>();
            //vals.Add("nzp_supp", "0");
            //vals.Add("nzp_kind", "1");
            //vals.Add("tn", "");

            vals.Add("nzp_supp", "10");
            vals.Add("nzp_frm", "21");
            vals.Add("tarif", "0");
            vals.Add("num_ls", "3344");

            editData.vals = vals;

            //cli_EditInterData cli = new cli_EditInterData();
            //cli.Saver(editData, out ret);

            DbEditInterData db = new DbEditInterData();
            db.Saver(editData, out ret);
            db.Close();

            return ret.text;
        }
        public string MustCalc()
        {
            Returns ret;

            DbCalc db2 = new DbCalc();
            //db2.CalcFull(0, "smr4", 2012, 2, 2012, 2, new bool[] { false, false, false, false, false, false, true }, out ret);
            //db2.CalcFull(0, "smr3", 2012, 2, 2012, 2, new bool[] { false, false, false, false, false, false, true }, out ret);
            //db2.CalcFull(0, "smr2", 2012, 2, 2012, 2, new bool[] { false, false, false, false, false, false, true }, out ret);
            //db2.CalcFull(0, "smr1", 2012, 2, 2012, 2, new bool[] { false, false, false, false, false, false, true }, out ret);

            //db2.CalcFull(0, "smr4", 2012, 1, 2012, 2, new bool[] { true, true, true, true, true, true, false, true }, out ret);

            //db2.CalcLs(951, "smr1", 2012, 2, 2012, 2, false, out ret);
            db2.CalcBaseWithReval(0, 0, "smr4", 2012, 2, out ret);


            db2.Close();

            return ret.text;

            /*
            EditInterData editData = new EditInterData();

            editData.nzp_wp = 11;
            editData.pref = "vas";
            editData.nzp_user = 1;
            editData.webLogin = "system";
            editData.webUname = "webuser";

            //editData.table = "tarif";
            //editData.primary = "nzp_tarif";
            //editData.mcalcType = enMustCalcType.mcalc_Serv;

            editData.table = "prm_2";
            editData.primary = "nzp_key";
            editData.mcalcType = enMustCalcType.mcalc_Prm2;
 
            editData.dat_s = "01.02.2011";
            editData.dat_po = "28.02.2011";
            editData.intvType = enIntvType.intv_Day;

            //условие выборки данных из целевой таблицы
            editData.dopFind = new List<string>();
            //editData.dopFind.Add(" and nzp_kvar = 300160 and nzp_serv = 9 ");
            editData.dopFind.Add(" and p.nzp = 300004 ");

            Dictionary<string, string> keys = new Dictionary<string, string>();
            keys.Add("-", "0");
            editData.keys = keys;
            Dictionary<string, string> vals = new Dictionary<string, string>();
            vals.Add("-", "0");
            editData.vals = vals;

            DbEditInterData db = new DbEditInterData();
            db.MustCalc(editData, out ret);
            db.Close();

            return ret.text;
            */
        }


        //-------------------------------------------------------------------------
        string Go(int kod, int year, int nzp_server, DbAnaliz db_anl, DbAnaliz.CreateTable createTable, out Returns ret)
        //-------------------------------------------------------------------------
        {
            Finder finder = new Finder();
            finder.nzp_server = nzp_server;
            finder.rows = 1000;

            string table = "";
            switch (kod)
            {
                case 1: table = "anl" + year; break;
                case 2: table = "anl" + year + "_dom"; break;
                case 3: table = "anl" + year + "_supp"; break;
                case 4: table = "supplier"; break;
                case 5: table = "services"; break;
                case 6: table = "s_point"; break;
                case 7: table = "s_area"; break;
                case 8: table = "s_geu"; break;
                case 9: table = "s_payer"; break;
            }

            int loaded = 0; //всего загружено
            finder.database = table + "_" + nzp_server;
            //++++++++++++++++++++++++++++++
            //создать образы целевых таблиц
            //++++++++++++++++++++++++++++++
            db_anl.CreateOrRenameAnlTable(finder.database, true, createTable, out ret);
            if (!ret.result)
            {
                return "Ошибка: " + ret.text;
            }
            //++++++++++++++++++++++++++++++
            //скачать данные по 1000 строк
            //++++++++++++++++++++++++++++++
            finder.skip = 0;
            while (true)
            {
                //скачать
                object ls_xx = new object();

                finder.database = table;
                switch (kod)
                {
                    case 1:
                        {
                            cli_Analiz cli = new cli_Analiz(finder.nzp_server);
                            ls_xx = cli.GetAnlXX(finder, out ret);
                            break;
                        }
                    case 2:
                        {
                            cli_Analiz cli = new cli_Analiz(finder.nzp_server);
                            ls_xx = cli.GetAnlDom(finder, out ret);
                            break;
                        }
                    case 3:
                        {
                            cli_Analiz cli = new cli_Analiz(finder.nzp_server);
                            ls_xx = cli.GetAnlSupp(finder, out ret);
                            break;
                        }
                    case 4:
                        {
                            cli_Sprav cli = new cli_Sprav(finder.nzp_server);
                            ls_xx = cli.SupplierLoad(finder, enTypeOfSupp.None, out ret);
                            break;
                        }
                    case 5:
                        {
                            cli_Sprav cli = new cli_Sprav(finder.nzp_server);
                            ls_xx = cli.ServiceLoad(finder, out ret);
                            break;
                        }
                    case 6:
                        {
                            cli_Sprav cli = new cli_Sprav(finder.nzp_server);
                            ls_xx = cli.PointLoad_WebData(finder, out ret);
                            break;
                        }
                    case 7:
                        {
                            cli_AdresHard cli = new cli_AdresHard(finder.nzp_server);
                            ls_xx = cli.GetArea(finder, out ret);
                            break;
                        }
                    case 8:
                        {
                            cli_Adres cli = new cli_Adres(finder.nzp_server);
                            ls_xx = cli.GetGeu(finder, out ret);
                            break;
                        }
                    case 9:
                        {
                            Payer payer = new Payer();
                            payer.nzp_server = finder.nzp_server;
                            payer.rows = finder.rows;
                            payer.skip = finder.skip;

                            cli_Sprav cli = new cli_Sprav(finder.nzp_server);
                            ls_xx = cli.PayerBankLoad(payer, enSrvOper.Bank, out ret);
                            break;
                        }
                }
                if (!ret.result)
                {
                    return "Ошибка: " + ret.text;
                }
                int cnt = ret.tag; //сколько всего записей должно быть

                //и сразу загрузить в базу
                finder.database = table + "_" + finder.nzp_server;
                switch (kod)
                {
                    case 1:
                        {
                            List<AnlXX> ls = new List<AnlXX>();
                            ls = (List<AnlXX>)ls_xx;
                            db_anl.LoadAnlXX(ls, finder, out ret);
                            break;
                        }
                    case 2:
                        {
                            List<AnlDom> ls = new List<AnlDom>();
                            ls = (List<AnlDom>)ls_xx;
                            db_anl.LoadAnlDom(ls, finder, out ret);
                            break;
                        }
                    case 3:
                        {
                            List<AnlSupp> ls = new List<AnlSupp>();
                            ls = (List<AnlSupp>)ls_xx;
                            db_anl.LoadAnlSupp(ls, finder, out ret);
                            break;
                        }
                    case 4:
                        {
                            List<_Supplier> ls = new List<_Supplier>();
                            ls = (List<_Supplier>)ls_xx;

                            DbSprav db = new DbSprav();
                            db.SupplierLoadInWeb(ls, finder.database, out ret);
                            db.Close();
                            break;
                        }
                    case 5:
                        {
                            List<_Service> ls = new List<_Service>();
                            ls = (List<_Service>)ls_xx;

                            DbSprav db = new DbSprav();
                            db.ServiceLoadInWeb(ls, finder.database, out ret);
                            db.Close();
                            break;
                        }
                    case 6:
                        {
                            List<_Point> ls = new List<_Point>();
                            ls = (List<_Point>)ls_xx;

                            DbSprav db = new DbSprav();
                            db.PointLoadInWeb(ls, finder.database, out ret);
                            db.Close();
                            break;
                        }
                    case 7:
                        {
                            List<_Area> ls = new List<_Area>();
                            ls = (List<_Area>)ls_xx;

                            using (var db = new DbAdresClient())
                            {
                                db.LoadAreaInWeb(ls, finder.database, out ret);
                            }
                            break;
                        }
                    case 8:
                        {
                            List<_Geu> ls = new List<_Geu>();
                            ls = (List<_Geu>)ls_xx;

                            using (var db = new DbAdresClient())
                            {
                                db.LoadGeuInWeb(ls, finder.database, out ret);
                            }
                            break;
                        }
                    case 9:
                        {
                            List<Payer> ls = new List<Payer>();
                            ls = (List<Payer>)ls_xx;

                            DbSprav db = new DbSprav();
                            db.PayerLoadInWeb(ls, finder.database, out ret);
                            db.Close();
                            break;
                        }
                }
                if (!ret.result)
                {
                    return "Ошибка: " + ret.text;
                }


                loaded += 1000;
                finder.skip = loaded;

                if (cnt <= loaded) break;
            }
            //++++++++++++++++++++++++++++++
            //переименовать таблицы
            //++++++++++++++++++++++++++++++
            db_anl.CreateOrRenameAnlTable(finder.database, false, createTable, out ret);
            if (!ret.result)
            {
                return "Ошибка: " + ret.text;
            }

            return "";

        }

        //-------------------------------------------------------------------------
        public string Go(int nzp_server)
        //-------------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            ThControl.thc[nzp_server] = true;

            DbAnaliz db_anl = new DbAnaliz();
            DbSprav db_sprav = new DbSprav();

            string s = Go(1, 2012, nzp_server, db_anl, db_anl.CreateAnlXX, out ret);
            if (!ret.result)
            {
                goto l;
            }
            s = Go(2, 2012, nzp_server, db_anl, db_anl.CreateAnlDom, out ret);
            if (!ret.result)
            {
                goto l;
            }
            s = Go(3, 2012, nzp_server, db_anl, db_anl.CreateAnlSupp, out ret);
            if (!ret.result)
            {
                goto l;
            }
            s = Go(4, 2012, nzp_server, db_anl, db_sprav.CreateWebSupplier, out ret);
            if (!ret.result)
            {
                goto l;
            }
            s = Go(5, 2012, nzp_server, db_anl, db_sprav.CreateWebService, out ret);
            if (!ret.result)
            {
                goto l;
            }
            s = Go(6, 2012, nzp_server, db_anl, db_sprav.CreateWebPoint, out ret);
            if (!ret.result)
            {
                goto l;
            }
            using (var db = new DbAdresClient())
            {
                s = Go(7, 2012, nzp_server, db_anl, db.CreateWebArea, out ret);
                if (!ret.result)
                {
                    goto l;
                }
                s = Go(8, 2012, nzp_server, db_anl, db.CreateWebGeu, out ret);
                if (!ret.result)
                {
                    goto l;
                }
            }
            s = Go(9, 2012, nzp_server, db_anl, db_sprav.CreateWebPayer, out ret);
            if (!ret.result)
            {
                goto l;
            }

        l:
            db_anl.Close();
            db_sprav.Close();

            ThControl.thc[nzp_server] = false;
            return s;

        }
        //-------------------------------------------------------------------------
        public string GetAnlXX()
        //-------------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            //загрузка доступных хостов
            DbMultiHostClient db_sprav = new DbMultiHostClient();
            ret = db_sprav.LoadMultiHost();
            db_sprav.Close();

            if (!ret.result)
                return "Ошибка: " + ret.text;

            MultiHost.IsMultiHost = true;

            //открываем цикл по серверам БД
            foreach (_RServer server in MultiHost.RServers)
            {
                int nzp_server = server.nzp_server;
                System.Threading.Thread thServer =
                                new System.Threading.Thread(delegate() { Go(nzp_server); });
                thServer.Start();
            }


            while (true)
            {
                bool b = true;
                foreach (_RServer server in MultiHost.RServers)
                {
                    int nzp_server = server.nzp_server;
                    if (ThControl.thc[nzp_server])
                    {
                        b = false;
                        break;
                    }
                }
                if (b) break;
            }

            return "OK!";
        }

        //-------------------------------------------------------------------------
        public string GetAnlXX2()
        //-------------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            string anlxx = "anl" + 2012;
            string anldom = "anl" + 2012 + "_dom";
            string anlsupp = "anl" + 2012 + "_supp";
            string supplier = "supplier";

            //загрузка доступных хостов
            DbMultiHostClient db_multi = new DbMultiHostClient();
            ret = db_multi.LoadMultiHost();
            db_multi.Close();

            DbSprav db_sprav = new DbSprav();

            if (!ret.result)
                return "Ошибка: " + ret.text;

            MultiHost.IsMultiHost = true;
            HostBase.timespan = "20:59:59";  //увеличить таймаут

            DbAnaliz db_anl = new DbAnaliz();
            DbAdres db_adres = new DbAdres();

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //открываем цикл по серверам
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            foreach (_RServer zap in MultiHost.RServers)
            {
                Finder finder = new Finder();
                finder.nzp_server = zap.nzp_server;
                finder.rows = 1000;

                List<AnlXX> ls_xx = new List<AnlXX>();
                List<AnlDom> ls_dom = new List<AnlDom>();
                List<AnlSupp> ls_supp = new List<AnlSupp>();

                //AnlXX ------------!!!!
                int loaded = 0; //всего загружено
                finder.database = anlxx + "_" + finder.nzp_server;
                //++++++++++++++++++++++++++++++
                //создать образы целевых таблиц
                //++++++++++++++++++++++++++++++
                db_anl.CreateOrRenameAnlTable(finder.database, true, db_anl.CreateAnlXX, out ret);
                if (!ret.result)
                {
                    db_anl.Close();
                    db_sprav.Close();
                    db_adres.Close();
                    return "Ошибка: " + ret.text;
                }
                //++++++++++++++++++++++++++++++
                //скачать данные по 1000 строк
                //++++++++++++++++++++++++++++++
                finder.skip = 0;
                while (true)
                {
                    //скачать
                    cli_Analiz cli = new cli_Analiz(finder.nzp_server);
                    finder.database = anlxx;
                    ls_xx = cli.GetAnlXX(finder, out ret);
                    if (!ret.result)
                    {
                        db_anl.Close();
                        db_sprav.Close();
                        db_adres.Close();
                        return "Ошибка: " + ret.text;
                    }
                    int cnt = ret.tag; //сколько всего записей должно быть

                    //и сразу загрузить в базу
                    finder.database = anlxx + "_" + finder.nzp_server;
                    db_anl.LoadAnlXX(ls_xx, finder, out ret);
                    if (!ret.result)
                    {
                        db_anl.Close();
                        db_sprav.Close();
                        db_adres.Close();
                        return "Ошибка: " + ret.text;
                    }

                    loaded += 1000;
                    finder.skip = loaded;

                    if (cnt <= loaded) break;
                }
                //++++++++++++++++++++++++++++++
                //переименовать таблицы
                //++++++++++++++++++++++++++++++
                db_anl.CreateOrRenameAnlTable(finder.database, false, db_anl.CreateAnlXX, out ret);
                if (!ret.result)
                {
                    db_anl.Close();
                    db_sprav.Close();
                    db_adres.Close();
                    return "Ошибка: " + ret.text;
                }


                //AnlXX_Dom ------------!!!!
                loaded = 0; //всего загружено
                finder.database = anldom + "_" + finder.nzp_server;
                db_anl.CreateOrRenameAnlTable(finder.database, true, db_anl.CreateAnlDom, out ret);
                if (!ret.result)
                {
                    db_anl.Close();
                    db_sprav.Close();
                    db_adres.Close();
                    return "Ошибка: " + ret.text;
                }
                finder.skip = 0;
                while (true)
                {
                    //скачать
                    cli_Analiz cli = new cli_Analiz(finder.nzp_server);
                    finder.database = anldom;
                    ls_dom = cli.GetAnlDom(finder, out ret);
                    if (!ret.result)
                    {
                        db_anl.Close();
                        db_sprav.Close();
                        db_adres.Close();
                        return "Ошибка: " + ret.text;
                    }
                    int cnt = ret.tag; //сколько всего записей должно быть

                    //и сразу загрузить в базу
                    finder.database = anldom + "_" + finder.nzp_server;
                    db_anl.LoadAnlDom(ls_dom, finder, out ret);
                    if (!ret.result)
                    {
                        db_anl.Close();
                        db_sprav.Close();
                        db_adres.Close();
                        return "Ошибка: " + ret.text;
                    }

                    loaded += 1000;
                    finder.skip = loaded;

                    if (cnt <= loaded) break;
                }
                db_anl.CreateOrRenameAnlTable(finder.database, false, db_anl.CreateAnlDom, out ret);
                if (!ret.result)
                {
                    db_anl.Close();
                    db_sprav.Close();
                    db_adres.Close();
                    return "Ошибка: " + ret.text;
                }


                //AnlXX_Supp ------------!!!!
                loaded = 0; //всего загружено
                finder.database = anlsupp + "_" + finder.nzp_server;
                db_anl.CreateOrRenameAnlTable(finder.database, true, db_anl.CreateAnlSupp, out ret);
                if (!ret.result)
                {
                    db_anl.Close();
                    db_sprav.Close();
                    db_adres.Close();
                    return "Ошибка: " + ret.text;
                }
                finder.skip = 0;

                while (true)
                {
                    //скачать
                    cli_Analiz cli = new cli_Analiz(finder.nzp_server);
                    finder.database = anlsupp;
                    ls_supp = cli.GetAnlSupp(finder, out ret);
                    if (!ret.result)
                    {
                        db_anl.Close();
                        db_sprav.Close();
                        db_adres.Close();
                        return "Ошибка: " + ret.text;
                    }
                    int cnt = ret.tag; //сколько всего записей должно быть

                    //и сразу загрузить в базу
                    finder.database = anlsupp + "_" + finder.nzp_server;
                    db_anl.LoadAnlSupp(ls_supp, finder, out ret);
                    if (!ret.result)
                    {
                        db_sprav.Close();
                        db_adres.Close();
                        db_anl.Close();
                        return "Ошибка: " + ret.text;
                    }

                    loaded += 1000;
                    finder.skip = loaded;

                    if (cnt <= loaded) break;
                }
                db_anl.CreateOrRenameAnlTable(finder.database, false, db_anl.CreateAnlSupp, out ret);
                if (!ret.result)
                {
                    db_anl.Close();
                    db_sprav.Close();
                    db_adres.Close();
                    return "Ошибка: " + ret.text;
                }





                List<_Supplier> ls_supplier = new List<_Supplier>();
                List<_Service> ls_service = new List<_Service>();
                List<_Point> ls_point = new List<_Point>();
                List<_Area> ls_area = new List<_Area>();
                List<_Geu> ls_geu = new List<_Geu>();

                //supplier ------------!!!!
                loaded = 0; //всего загружено
                finder.database = supplier + "_" + finder.nzp_server;
                db_anl.CreateOrRenameAnlTable(finder.database, true, db_sprav.CreateWebSupplier, out ret);
                if (!ret.result)
                {
                    db_anl.Close();
                    db_sprav.Close();
                    db_adres.Close();
                    return "Ошибка: " + ret.text;
                }
                finder.skip = 0;

                while (true)
                {
                    //скачать
                    cli_Sprav cli = new cli_Sprav(finder.nzp_server);
                    ls_supplier = cli.SupplierLoad(finder, enTypeOfSupp.None, out ret);
                    if (!ret.result)
                    {
                        db_anl.Close();
                        db_sprav.Close();
                        db_adres.Close();
                        return "Ошибка: " + ret.text;
                    }
                    int cnt = ret.tag; //сколько всего записей должно быть

                    //и сразу загрузить в базу
                    db_sprav.SupplierLoadInWeb(ls_supplier, finder.database, out ret);
                    if (!ret.result)
                    {
                        db_anl.Close();
                        db_sprav.Close();
                        db_adres.Close();
                        return "Ошибка: " + ret.text;
                    }

                    loaded += 1000;
                    finder.skip = loaded;

                    if (cnt <= loaded) break;
                }
                db_anl.CreateOrRenameAnlTable(finder.database, false, db_sprav.CreateWebSupplier, out ret);
                if (!ret.result)
                {
                    db_anl.Close();
                    db_sprav.Close();
                    db_adres.Close();
                    return "Ошибка: " + ret.text;
                }




            }

            db_anl.Close();
            db_sprav.Close();
            db_adres.Close();

            return "ОК: " + ret.text + ".....";
        }

        public string PackXX(bool to_dis)
        {
            Returns ret = Utils.InitReturns();

            DbCalcPack db2 = new DbCalcPack();
            //db2.PackFonTasks(100154, to_dis, out ret);

            DateTime d = new DateTime(2012, 6, 15);
            if (ret.result)
            {
                TransferBalanceFinder finder = new TransferBalanceFinder() 
                {
                    dat_s = d.ToShortDateString(),
                    dat_po = d.ToShortDateString()
                };

                db2.DistribPaXX_1(finder, out ret, null);
            }


            /*
            if (ret.result)
                db2.DistribPaXX(d.AddDays(1), out ret);
            
            if (ret.result)
                db2.CalcSaldo(out ret);
            */

            db2.Close();

            return ret.text;
        }

        public string Test0()
        {
            Returns ret = Utils.InitReturns();

            DbCalc db2 = new DbCalc();
            db2.CalcLs(134002240, "sav08", 2012, 6, 2012, 6, true, false, "", 0, out ret);
            db2.Close();

            return ret.text;
        }

        public string Test01()
        {
            Returns ret = Utils.InitReturns();

            DbGenerator db2 = new DbGenerator();

            List<int> lFields = new List<int>();
            //lFields.Add(13);
            //lFields.Add(14);
            lFields.Add(11);
            lFields.Add(0);
            lFields.Add(1);
            lFields.Add(3);
            lFields.Add(2);
            lFields.Add(10);

            List<int> lServ = new List<int>();
            //lServ.Add(15);
            //lServ.Add(2);

            db2.GenCharge(lFields, lServ, 1, 2012, 5, true, true);
            db2.Close();

            return ret.text;
        }

        /// <summary>
        /// Получение конфигурации
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public List<ConfigKey> LoadNewConfig(List<string> param)
        {
            var dGrid = new List<ConfigKey>();
            var regex = new Regex(RegexConfig);
            foreach (var p in param)
            {
                if (string.IsNullOrEmpty(p))
                {
                    continue;
                }
                var match = regex.Match(p);
                if (!match.Success)
                {
                    continue;
                }
                var conf = new ConfigKey
                               {
                                   key = match.Groups["key"].ToString(),
                                   val = Encryptor.Decrypt(match.Groups["value"].ToString(), null)
                               };
                dGrid.Add(conf);
            }
            return dGrid;
        }

        public string SaveToCMDFile(List<ConfigKey> dGrid, enGrid grid, string fileName) {
            //bool b = true;
            string server = string.Empty, database = string.Empty;
            try
            {
                var keyValue = dGrid.Find(x => x.key == "W1");
                string value = keyValue.val;
                int indexServer = value.IndexOf("server", StringComparison.OrdinalIgnoreCase) + 6;
                if (indexServer != -1)
                {
                    value = value.Substring(indexServer);
                    int indexZnak = value.IndexOf(';');
                    if (indexZnak != -1)
                    {
                        server = value.Remove(indexZnak).TrimStart('=');

                        value = keyValue.val;

                        int indexDatabase = value.IndexOf("database", StringComparison.OrdinalIgnoreCase) + 8;

                        if (indexDatabase != -1)
                        {
                            value = value.Substring(indexDatabase);
                            indexZnak = value.IndexOf(';');
                            if (indexZnak != -1)
                            {
                                database = value.Remove(indexZnak).TrimStart('=');
                            }
                        }
                    }
                }

                if (server != string.Empty && database != string.Empty)
                {
                    fileName += "\\" + server + "_" + database + ".cmd";
                }

                using (var sw = new StreamWriter(fileName, false))
                {
                    string beginFile = string.Empty, endFile = "echo ^<appSettings^> >> %filename%\n";
                    foreach (ConfigKey zap in dGrid)
                    {
                        beginFile += @"SET " + zap.key + "=\"" + Encryptor.Encrypt(zap.val, null) + "\"\n";
                        endFile += "echo ^<add key=\"" + zap.key + "\" value=%" + zap.key + "% /^> >> %filename%\n";

                    }
                    endFile += "echo ^</appSettings^> >> %filename%\n";

                    sw.WriteLine(beginFile);
                    sw.WriteLine("SET filename=\"Host.user.config\"");
                    sw.WriteLine("del %filename%\n");
                    sw.WriteLine(endFile);
                }
            }
            catch (Exception ex)
            {
                return "Ошибка формирование cmd-файла: " + ex.Message;
            }

            return "cmd-файл сформирован";
        }
    }

    //public string SaveToFile(List<ConfigKey> dGrid, enGrid grid, string filename)
    //    {
    //        bool b = true;
    //        foreach (ChangeVal zap in dGrid)
    //        {
    //            if (zap.changed)
    //            {
    //                b = false;
    //                break;
    //            }
    //        }
    //        if (b) return "Изменения не обнаружены";

    //        string f_head = "connectionStrings";
    //        string f_key = "name";
    //        string f_val = "connectionString";

    //        if (grid == enGrid.NewHost)
    //        {
    //            f_head = "appSettings";
    //            f_key = "key";
    //            f_val = "value";
    //        }

    //        try
    //        {
    //            FileInfo fi = new FileInfo(filename);
    //            fi.Attributes = FileAttributes.Normal;
    //            fi.CopyTo(filename + ".save", true);
    //            fi.Delete();

    //            using (StreamWriter sw = new StreamWriter(filename, false))
    //            {
    //                sw.WriteLine(@"<" + f_head + ">");
    //                foreach (ConfigKey zap in dGrid)
    //                {
    //                    sw.WriteLine(@"  <add " + f_key + "=\"" + zap.key + "\" " + f_val + "=\"" + Encryptor.Encrypt(zap.val, null) + "\" />");
    //                }
    //                sw.WriteLine(@"</" + f_head + ">");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            return "Ошибка записи в файл: " + ex.Message;
    //        }

    //        return "Данные успешно сохранены";
    //    }

    public static class ThControl
    {

        public static bool[] thc = new bool[100];

    }

}
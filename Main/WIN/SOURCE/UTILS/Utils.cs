//--------------------------------------------------------------------------------80
//Файл: utils.cs
//Дата создания: 14.05.2009
//Дата изменения: 15.05.2009
//Назначение: Полезные методы (статичные)
//Автор: Зыкин А.А.
//Copyright (c) Научно-технический центр "Лайн", 2011. 
//--------------------------------------------------------------------------------80
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Windows.Forms;
using System.Drawing;
using System.IO;


namespace STCLINE.KP50.WinUtils
{
    class Utils
    {
        //----------------------------------------------------------------------
        //статичный метод. Выводит сообщение
        // Зыкин А.А.
        //----------------------------------------------------------------------
        static public void HideWait(Form frm)
        {
            frm.Close();

        }
        static public Form ShowWait(Form frm, string msg)
        {
            if (frm != null)
            {
                (frm.Controls[0].Controls[0] as Label).Text = msg;
                Application.DoEvents();
                return frm;
            }
            else
                return ShowWait(msg,250);

        }
        static public Form ShowWait(string msg)
        {
            return ShowWait(msg, 250);
        }
        static public Form ShowWait(string msg, int formWidth)
        {
            Form frm = new Form();
            frm.Text = msg;
            frm.StartPosition = FormStartPosition.CenterScreen;
            frm.Size = new Size(formWidth, 70);
            frm.ShowInTaskbar = false;
            frm.FormBorderStyle = FormBorderStyle.None;

            Panel pnl = new Panel();
            pnl.Parent = frm;
            pnl.BackColor = SystemColors.Info;
            pnl.Dock = DockStyle.Fill;
            pnl.BorderStyle = BorderStyle.FixedSingle;

            Label lbl = new Label();
            lbl.Parent = pnl;
            lbl.Text = msg;
            lbl.AutoSize = true;
            lbl.Location = new Point(37, 23);
            lbl.Font = new Font(FontFamily.GenericSansSerif, 12.0F);

            frm.Show();
            Application.DoEvents();

            return frm;

        }
        //----------------------------------------------------------------------
        //статичный метод. Создает экземпляр класса формы, настраивает его свойства и возвращает указатель
        // Зыкин А.А.
        //----------------------------------------------------------------------
        static public Form CreateForm(Form frm)
        {
            return CreateForm(frm, false);
        }

        static public Form CreateForm(Form frm, bool show)
        {
            frm.ShowInTaskbar = show;
            frm.ShowDialog();
//            frm.TopMost = true;
            return frm;

        }

        /*
        //----------------------------------------------------------------------
        //статичный метод. Выполнение отчета
        // Зыкин А.А.
        //----------------------------------------------------------------------
        static public void ShowReport()
        {
            FastReport.Report rep1 = new FastReport.Report();
            rep1.Show();
            rep1.Clear();        

        }

        static public void ShowReport(string reportName, Hashtable dsPar, bool showModal, Icon icon)
        {
            //------------------------------------------------
            Form frm = ClassUtils.ShowWait("Выполнение отчёта . . .");
            //------------------------------------------------

            FastReport.Report rep1 = new FastReport.Report();

            FastReport.EnvironmentSettings envSett = new FastReport.EnvironmentSettings();
            envSett.UIStyle = FastReport.Utils.UIStyle.VisualStudio2005;

            envSett.ReportSettings.ShowProgress = true;
            envSett.DesignerSettings.Icon = icon;
            envSett.PreviewSettings.ShowInTaskbar = true;
//            envSett.PreviewSettings.TopMost = true;


            //------------------------------------------------
            //вызов сервиса 
            //------------------------------------------------
            String s = ClassSession.remoteReport.ShowReport(ClassSession.userLogin, reportName, dsPar);
            MemoryStream memstr = new MemoryStream();


            memstr.SetLength(s.Length);
            memstr.Write(System.Text.Encoding.Default.GetBytes(s), 0, s.Length);
            memstr.Position = 0;
            //------------------------------------------------
            //------------------------------------------------
            ClassUtils.HideWait(frm);

            rep1.LoadPrepared(memstr);
            rep1.ShowPrepared(true);
            rep1.Clear();
        }
        */

        //----------------------------------------------------------------------
        //статичный метод. Сохранение в реестре переменной 
        // Зыкин А.А.
        //----------------------------------------------------------------------
        static public void SetRegistryValue(string paramName, string paramValue)
        {
            Microsoft.Win32.RegistryKey clRegistryKey;
            string subKey = @"Software\STC Line\TSRClient";
            //Проверяем есть ли наш подраздел
            clRegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKey, true);

            if (clRegistryKey == null)
            {
                //Раздела нет - создаем и открываем для чтения и добавления
                clRegistryKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(subKey);
            }

            clRegistryKey.SetValue(paramName, paramValue);
            clRegistryKey.Close();

        }
        //----------------------------------------------------------------------
        //статичный метод. Получение из реестра переменной 
        // Зыкин А.А.
        //----------------------------------------------------------------------
        static public string GetRegistryValue(string paramName)
        {
            Microsoft.Win32.RegistryKey clRegistryKey;
            string subKey = @"Software\STC Line\TSRClient";
            //Проверяем есть ли наш подраздел
            clRegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKey);
            if (clRegistryKey == null)
                return "";
            else
            {
                string res = (string)clRegistryKey.GetValue(paramName);
                clRegistryKey.Close();
                return res;
            }
        }
    }
}

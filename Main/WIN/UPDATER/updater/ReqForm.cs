using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ServiceModel;
using SevenZip;
using System.Collections;
using System.Security.Cryptography;
using System.Threading;
using System.Configuration;
using System.ServiceModel.Configuration;

namespace updater
{
    public partial class ReqForm : Form
    {
        ArrayList rajs_ip = new ArrayList();
        ArrayList rajs_name = new ArrayList();

        public ReqForm(string rajons, ArrayList connect, ArrayList rajs)
        {
            InitializeComponent();
            this.Text = rajons;
            radioButton1.Text = "webdata";
            radioButton2.Text = "kernel";
            rajs_ip = connect;
            rajs_name = rajs;
        }


        private void FileOpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ArrayList arr = new ArrayList();
            OpenFileDialog filedialog = new OpenFileDialog();
            if (filedialog.ShowDialog() == DialogResult.OK)
            {
                string filename = filedialog.FileName;
                StreamReader str = new StreamReader(filename, Encoding.GetEncoding(1251));
                while (!str.EndOfStream)
                {
                    string stok =  str.ReadLine();
                    while (stok == "")
                    {
                        stok = str.ReadLine();
                    }
                    arr.Add(stok);
                }
                string[] st = (String[])arr.ToArray(typeof(string));
                richTextBox1.Lines = st;
            }
        }
        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void button1_Click(object sender, EventArgs e)
        {
            ArrayList sql_str = new ArrayList ();//запрос

            ArrayList sql_str2 = new ArrayList();

            //ключ для шифрования ключа к архиву
            string encryptrequestkey = "4PSckw3IcgQ/at00qGJ2RPcvDvmr=UfjcSm64cXLycw";

            string soup = "";//дополнительный генерируемый ключ для зашифрования запроса
            int soup_length = 10;//длина передаваемого ключа

            //ключ для шифрования запроса
            string daykey = "";
            int daykey_lenght = 16;

            string base_str = "";//строка подключения к БД
            if (richTextBox1.Text != "")
            {
                if (radioButton1.Checked || radioButton2.Checked)
                {
                    #region Заполнение отправляемых данных

                    if (radioButton1.Checked)
                    {
                        base_str = radioButton1.Text;
                    }
                    else
                    {
                        base_str = radioButton2.Text;
                    }
                  
                    //построчно добавляем запрос
                    soup = Password.GeneratePassword(soup_length);
                    daykey = Password.GeneratePassword(daykey_lenght);

                    daykey = Crypt.Encrypt(daykey, (encryptrequestkey + soup));

                    for (int p = 0; p < richTextBox1.Lines.Length; p++)
                    {
                        if (richTextBox1.Lines[p] != " " && richTextBox1.Lines[p].Length != 0)
                        {
                            sql_str.Add(Crypt.Encrypt(richTextBox1.Lines[p], daykey));
                        }
                    }

                    byte[] salt = new UTF8Encoding().GetBytes(soup);

                    #endregion

                    //куда сохраняем дынные из районов
                    SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                    saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                    saveFileDialog1.RestoreDirectory = true;

                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        for (int tp = 0; tp < rajs_ip.Count; tp++)
                        {
                            int j = tp;
                            ThreadPool.QueueUserWorkItem(delegate(object notUsed) { RequestToRajons(rajs_ip[j].ToString(), rajs_name[j].ToString(), sql_str, base_str, saveFileDialog1, j, salt); });
                        }
                    }
                    
                }
                else
                {
                    MessageBox.Show("Выберите базу данных!");
                }
            }
        }

        public void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (richTextBox1.Text != "")
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        //Создаём или перезаписываем существующий файл
                        StreamWriter sw = File.CreateText(saveFileDialog1.FileName);
                        //Записываем текст в поток файла
                        for (int i = 0; i < richTextBox1.Lines.Length; i++)
                        {
                            string a = richTextBox1.Lines[i].ToString();
                            sw.WriteLine(a);
                        }
                        //Закрываем файл
                        sw.Close();
                    }
                    catch (Exception ex) //хэндлим ошибки
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }

                }
            }
            else
            {
                MessageBox.Show("Сначала введите данные");
            }
        }


        static public void RequestToRajons(string rajs_ip, string rajs_name, ArrayList sql, string base_str, SaveFileDialog saver, int k, byte[] salt)
        {
            Object thisLock = new Object();

            lock (thisLock)
            {
                //string base_name = "";
                //if (base_str == "webdata")
                //{
                //    base_name = "cache";
                //}
                Dictionary<string, object> lib = new Dictionary<string, object>();
                ArrayList m1 = new ArrayList();
                ArrayList m2 = new ArrayList();
                bool checker = false;

                //проверка соединений с районами
                //checker = Check.CheckConnect(rajs_name, rajs_ip + MainForm.service_connection);

                if (checker == true)
                {
                    //соединения с районами
                    //using (updater.ServiceReference1.UpdatersClient myclient = new updater.ServiceReference1.UpdatersClient())
                    //{
                    //    try
                    //    {
                    //        EndpointAddress endp = new EndpointAddress(rajs_ip + MainForm.service_connection);
                    //        myclient.Endpoint.Address = endp;
                    //        myclient.Open();
                    //        lib = myclient.GetPatchResult(sql.ToArray(), base_name, salt);
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        MessageBox.Show("Ошибка при соединении с районом: " + rajs_name + "; Ошибка:" + ex.Message);
                    //    }
                    //}
                    if (lib != null)
                    {   
                        foreach (KeyValuePair<string, object> temp in lib)
                        {
                            if (temp.Key.Contains("#error"))
                            {
                                MessageBox.Show(temp.Value.ToString());
                            }
                            string str_temp = "";
                            str_temp = temp.Key.Remove(temp.Key.LastIndexOf('#'));
                            m1.Add(str_temp);
                            m2.Add(temp.Value);
                        }
                        string filename = saver.FileName.Remove(saver.FileName.LastIndexOf('.'));
                        //string filename = saver.FileName.Substring(saver.FileName.LastIndexOf('.'));
                        StreamWriter sw = File.CreateText(filename + "_" + k + ".txt");
                        //Записываем текст в поток файла
                        for (int i = 0; i < m1.Count; i++)
                        {
                            sw.WriteLine(m1[i] + " " + m2[i]);
                        }
                        //Закрываем файл
                        sw.Close();
                    }
                    else
                    {
                        MessageBox.Show("Возвращаемые данные пусты!");
                    }
                }
            }
        }

        #region Обработка запроса
        static public string Parser(ArrayList text)
        {
            //формируем массив всех вызовов функций
            ArrayList functions = new ArrayList();
            for (int i = 0; i < text.Count; i++)
            {
                if(text[i].ToString().Contains("#function"))
                {
                    functions.Add(text[i].ToString());
                }
            }


            for (int j = 0; j < functions.Count; j++)
            {
                for (int k = 0; k < text.Count; k++)
                {
                    //ищем вхождение вызова
                }
            }

                return "";
        }
        #endregion
    }
}

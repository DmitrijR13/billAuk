using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.ServiceModel;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Client;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using VersionCompile;

namespace updater
{
    public partial class RajonForm : Form
    {
        UpData2[] List_update;
        string connect_str = "";
        string login = "";
        string password = "";
        public RajonForm(Info item)
        {
            InitializeComponent();
            string column1name = "Район";
            string column2name = "Статус";
            string column3name = "Тип";
            string column4name = "Версия";
            string column5name = "Дата";
            string column6name = "Отчет";
            dgv_rajon_history.Columns.Add(column1name, "Район");
            dgv_rajon_history.Columns.Add(column2name, "Статус обновления");
            dgv_rajon_history.Columns.Add(column3name, "Тип обновления");
            dgv_rajon_history.Columns.Add(column4name, "Версия обновления");
            dgv_rajon_history.Columns.Add(column5name, "Дата обновления");
            dgv_rajon_history.Columns.Add(column6name, "Отчет обновления");

            dgv_rajon_history.Columns[0].Visible = false;

            dgv_rajon_history.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv_rajon_history.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv_rajon_history.Columns[1].DefaultCellStyle.Font = new Font("Arial", 14F, GraphicsUnit.Pixel);
            dgv_rajon_history.Columns[1].Width = 20;

            dgv_rajon_history.Columns[2].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv_rajon_history.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv_rajon_history.Columns[2].DefaultCellStyle.Font = new Font("Arial", 14F, GraphicsUnit.Pixel);
            dgv_rajon_history.Columns[2].Width = 50;

            dgv_rajon_history.Columns[3].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv_rajon_history.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv_rajon_history.Columns[3].DefaultCellStyle.Font = new Font("Arial", 14F, GraphicsUnit.Pixel);
            dgv_rajon_history.Columns[3].Width = 50;

            dgv_rajon_history.Columns[4].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv_rajon_history.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv_rajon_history.Columns[4].DefaultCellStyle.Font = new Font("Arial", 14F, GraphicsUnit.Pixel);
            dgv_rajon_history.Columns[4].Width = 50;

            dgv_rajon_history.Columns[5].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv_rajon_history.Columns[5].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv_rajon_history.Columns[5].DefaultCellStyle.Font = new Font("Arial", 14F, GraphicsUnit.Pixel);
            dgv_rajon_history.Columns[5].Width = 50;

            connect_str = item.rajon_ip;
            login = item.rajon_login;
            password = item.rajon_password;
        }
        private void dataGridView2_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 5)
            {
                Report form3 = new Report();
                form3.Text = "История обновлений района";
                ArrayList rep = new ArrayList();
                Database DB = new Database();
                rep = DB.Get_rajon_info(PathsAndKeys.DB_Connect, "Select update_report from rajon_history where update_type =" + "\'" + dgv_rajon_history[2, e.RowIndex].Value.ToString() + "\' AND update_version=" + "\'" + dgv_rajon_history[3, e.RowIndex].Value.ToString() + "\' AND rajon_name=" + "\'" + dgv_rajon_history[0, e.RowIndex].Value.ToString() + "\'", 2);
                foreach (Info report in rep)
                {
                    form3.richTextBox1.Text = report.update_report;
                }
                form3.Show();
            }
        }

        private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Report form4 = new Report();
            form4.Text = "Справка статусов обновлений";
            form4.richTextBox1.Text = "\r\n---------------------------Статусы обновлений-----------------------------" +
            "\r\n0 - доступно, не установлено" + 
            "\r\n1 - успешно установлено" +
            "\r\n2 - обновление завершилось с ошибкой, но откат назад успешно выполнен" + 
            "\r\n3 - обновление завершилось с ошибкой, откат назад завершился с ошибкой" + 
            "\r\n4 - стутсы следующих в очереди обновлений, когда текущее обновление завершилось с ошибкой" + 
            "\r\n5- (только для WEB ) статусы следующих в очереди обновлений, если Host на очередном обновлении сломался" +
            "\r\n6 - Обновление файлов успешно завершено, удаление резервных данных ошибка." +
            "\r\n7 - Выполнение SQL скрипта завершилось неудачно." +
            "\r\n-----------------------------------------------------------------------------";
            form4.Show();
        }

        #region Обновление данных об обновлениях

        public void UploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (PathsAndKeys.DB_Connect == "")
            {
                MessageBox.Show("Настройте подключение к БД");
                return;
            }
            ArrayList data = new ArrayList();
            bool cheker = false;
            string connect = connect_str + MainForm.service_connection;
            //проверка соединения с районом
            Info chek = new Info();
            chek.rajon_ip = connect_str;
            chek.rajon_login = login;
            chek.rajon_password = password;

            cheker = Check.CheckConnect(chek);
            if (cheker)
            {
                cli_Patch cli = new cli_Patch(connect_str + "/srv", login, password);
                Stream stream = cli.GetHistoryFull();
                BinaryFormatter formatter = new BinaryFormatter();
                List_update = (UpData2[])(formatter.Deserialize(stream));

                Database DB = new Database();
                if (List_update != null)
                {
                    foreach (UpData2 ups in List_update)
                    {
                        Database db = new Database();
                        if (DB.Get_rajon_info(PathsAndKeys.DB_Connect, "SELECT  rajon_name, update_status, update_version, update_type, update_date FROM rajon_history WHERE rajon_name = " + "\'" + this.Text + "\' AND update_version = " + "\'" + ups.Version + "\' AND update_type = " + "\'" + ups.typeUp + "\' AND update_date = \'" + DateTime.Parse(ups.date).ToString("yyyy-MM-dd HH:mm:ss") + "\'", 0).Count == 0)
                        {
                            db.WriteUpdate(PathsAndKeys.DB_Connect, "INSERT INTO rajon_history VALUES (" + "\'" + this.Text + "\'," + "\'" + ups.status + "\'," + "\'" + ups.typeUp + "\'," + "\'" + ups.Version + "\'," + "\'" + DateTime.Parse(ups.date).ToString("yyyy-MM-dd HH:mm:ss") + "\', ?)", ups.report);
                        }
                    }
                    this.dgv_rajon_history.Rows.Clear();

                    ArrayList history = new ArrayList();
                    string sql_hist = "Select rajon_name, update_status, update_type, update_version, update_date from rajon_history where rajon_name =" + "\'" + this.Text + "\' and update_version <> 0 ORDER BY update_date DESC";
                    history = DB.Get_rajon_info(PathsAndKeys.DB_Connect, sql_hist, 0);
                    if (history.Count != 0)
                    {
                        foreach (Info raj in history)
                        {
                            Color color = Color.LightCoral;
                            if (raj.update_status == "1")
                            {
                                color = Color.LightGreen;
                            }
                            this.dgv_rajon_history.Rows.Add(raj.rajon_name, raj.update_status, raj.update_type, raj.update_version, raj.update_date, "Отчет");
                            for (int i = 0; i < this.dgv_rajon_history.ColumnCount; i++)
                            {
                                this.dgv_rajon_history.Rows[this.dgv_rajon_history.Rows.Count - 1].Cells[i].Style.BackColor = color;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("У района " + this.Text + " нет истории обновлений!");
                    }
                }
                else
                {
                    MessageBox.Show("Возвращаемые данные пусты!");
                }
            }
            else
            {
                MessageBox.Show("Район недоступен");
            }
        }
        #endregion 

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

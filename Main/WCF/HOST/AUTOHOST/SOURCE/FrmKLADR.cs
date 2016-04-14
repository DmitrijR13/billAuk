//http://www.microsoft.com/en-us/download/details.aspx?id=14839
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IBM.Data.Informix;
using System.Data.OleDb;
using STCLINE.KP50.DataBase;
using System.Threading;
using STCLINE.KP50.HostMan.Loading;
using STCLINE.KP50.Global;
using System.Configuration;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.HostMan.KLADR
{
    public partial class KLADR : Form
    {
        private string FileName; // путь + имя файла
        private string FilePath; // только путь
        //private string StreetFileName;
        private string region;
        private string district;
        private string city;
        private string settlement;

        private DataTable regionTable;
        private DataTable districtTable;
        private DataTable cityTable;
        private DataTable settlementTable;
        private DataTable streetTable;
        private string level;
        private bool streetAllow;
        private Loading.Loading loadForm;
        private Thread thread;
        //private List<ConfigKey> confList;
        private string ConnectionString;
        private string upper_bank;
        private List<string> bottom_banks;

        public KLADR(string DBFName, string tconnectionString)
        {
            InitializeComponent();
            //var words = DBFName.Split('\\');
            //for (var i = 0; i < words.Length - 1; i++)
            //{
            //    FileName += words[i] + "\\";
            //}
            FileName = DBFName;

            //определение пути
            FilePath = "";
            var words = FileName.Split('\\');
            for (var i = 0; i < words.Length - 1; i++)
            {
                FilePath += words[i] + "\\";
            }

            ConnectionString = tconnectionString;

            IfxConnection conn = new IfxConnection() { ConnectionString = ConnectionString };
            conn.Open();
            //определение верхнего банка
            string sqlString = "select bd_kernel from s_point where nzp_graj = 0";
            IfxCommand ifxCommand = new IfxCommand(sqlString, conn);
            upper_bank = ifxCommand.ExecuteScalar().ToString().Trim();
            ifxCommand.Dispose();

            //определение нижних банков
            sqlString = "select bd_kernel from s_point where nzp_graj = 1";
            ifxCommand = new IfxCommand(sqlString, conn);
            var reader = ifxCommand.ExecuteReader();
            ifxCommand.Dispose();
            bottom_banks = new List<string>();
            while (reader.Read())
            {
                bottom_banks.Add(reader["bd_kernel"].ToString().Trim());
            }

            streetAllow = false;

            regionTable = new DataTable();
            districtTable = new DataTable();
            cityTable = new DataTable();
            settlementTable = new DataTable();
            streetTable = new DataTable();
            streetTable.Columns.Add("FULLNAME", typeof(String));

            //получение списка регионов
            regionTable = readDBF("select * from kladr where mid(CODE, 3, 11) = '00000000000'");
            cbRegion.DataSource = regionTable.DefaultView;
            cbRegion.DisplayMember = "FULLNAME";
            cbRegion.ValueMember = "CODE";
            cbRegion.BindingContext = this.BindingContext;
            cbRegion.SelectedIndex = -1;
        }

        //выбран регион
        private void cbRegion_SelectionChangeCommitted(object sender, EventArgs e)
        {
            streetAllow = false;
            cbDistrict.Enabled = false;
            cbCity.Enabled = false;
            cbSettlement.Enabled = false;
            level = "region";

            DataRowView dr = (DataRowView)cbRegion.SelectedItem;
            region = dr.Row["CODE"].ToString().Substring(0, 2);

            //загружаем список районов
            districtTable.Clear();
            districtTable = readDBF("select * from kladr where mid(CODE, 1, 2) = '" + region + "' AND mid(CODE, 6, 6) = '000000' AND mid(CODE, 3, 3) <> '000' and mid(CODE, 12, 2) = '00'");

            if (districtTable.Rows.Count != 0)
            {
                cbDistrict.DataSource = districtTable.DefaultView;
                cbDistrict.DisplayMember = "FULLNAME";
                cbDistrict.ValueMember = "CODE";
                cbDistrict.BindingContext = this.BindingContext;
                cbDistrict.SelectedIndex = -1;

                cbDistrict.Enabled = true;
                btnClearDistrict.Enabled = true;
            }

            //загружаем список городов, не относящихся к районам
            cityTable = readDBF("select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                " AND mid(CODE, 3, 3) = '000' AND mid(CODE, 9, 3) = '000' AND mid(CODE, 6, 3) <> '000' and mid(CODE, 12, 2) = '00'");
            if (cityTable.Rows.Count != 0)
            {
                cbCity.DataSource = cityTable.DefaultView;
                cbCity.DisplayMember = "FULLNAME";
                cbCity.ValueMember = "CODE";
                cbCity.BindingContext = this.BindingContext;
                cbCity.SelectedIndex = -1;

                cbCity.Enabled = true;
                btnClearCity.Enabled = true;
            }

            if (cbxLoadStreet.Checked)
            {
                DataRowView dw = (DataRowView)cbRegion.SelectedItem;
                loadStreetTable(dw.Row, true, streetTable);
            }
        }

        //выбран район
        private void cbDistrict_SelectionChangeCommitted(object sender, EventArgs e)
        {
            cbCity.Enabled = false;
            btnClearCity.Enabled = false;
            cbSettlement.Enabled = false;
            cbStreet.Enabled = false;
            cbStreet.SelectedIndex = -1;
            btnClearStreet.Enabled = false;
            level = "district";

            DataRowView dr = (DataRowView)cbDistrict.SelectedItem;
            district = dr.Row["CODE"].ToString().Substring(2, 3);

            cityTable.Rows.Clear();
            cityTable = readDBF("select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                " AND mid(CODE, 3, 3) = '" + district + "'AND mid(CODE, 9, 3) = '000' AND mid(CODE, 6, 3) <> '000' and mid(CODE, 12, 2) = '00'");
            if (cityTable.Rows.Count == 0)
            {
                city = "000";
                loadSettlement();
            }
            else
            {
                cbCity.DataSource = cityTable.DefaultView;
                cbCity.DisplayMember = "FULLNAME";
                cbCity.ValueMember = "CODE";
                cbCity.BindingContext = this.BindingContext;
                cbCity.SelectedIndex = -1;

                cbCity.Enabled = true;
                btnClearCity.Enabled = true;

                string temp_city = city;
                city = "000";
                loadSettlement();
                city = temp_city;
            }
        }

        //выбран город
        private void cbCity_SelectionChangeCommitted(object sender, EventArgs e)
        {
            level = "city";
            DataRowView dr = (DataRowView)cbCity.SelectedItem;
            city = dr.Row["CODE"].ToString().Substring(5, 3);
            if (cbDistrict.SelectedIndex == -1)
            {
                cbDistrict.Enabled = false;
                btnClearDistrict.Enabled = false;
                district = "000";
            }
            if (cbxLoadStreet.Checked)
            {
                DataRowView dw = (DataRowView)cbCity.SelectedItem;
                loadStreetTable(dw.Row, true, streetTable);
            }
            loadSettlement();
        }

        //загрузка населенных пунктов
        private void loadSettlement()
        {
            cbSettlement.Enabled = false;
            btnClearSettlement.Enabled = false;

            settlementTable.Clear();
            settlementTable = readDBF("select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                " AND mid(CODE, 3, 3) = '" + district + "'AND mid(CODE, 6, 3) = '" + city + "' AND mid(CODE, 9, 3) <> '000' and mid(CODE, 12, 2) = '00'");
            if (settlementTable.Rows.Count == 0)
            {
                settlement = "000";
                streetAllow = true;
            }
            else
            {
                cbSettlement.DataSource = settlementTable.DefaultView;
                cbSettlement.DisplayMember = "FULLNAME";
                cbSettlement.ValueMember = "CODE";
                cbSettlement.BindingContext = this.BindingContext;
                cbSettlement.SelectedIndex = -1;

                cbSettlement.Enabled = true;
                btnClearSettlement.Enabled = true;
            }
        }

        //выбран населенный пункт
        private void cbSettlement_SelectionChangeCommitted(object sender, EventArgs e)
        {
            level = "settlement";
            streetAllow = true;

            DataRowView dr = (DataRowView)cbSettlement.SelectedItem;
            settlement = dr.Row["CODE"].ToString().Substring(8, 3);

            if (cbCity.SelectedIndex == -1)
            {
                cbCity.Enabled = false;
                btnClearCity.Enabled = false;
            }
            if (cbxLoadStreet.Checked)
            {
                DataRowView dw = (DataRowView)cbSettlement.SelectedItem;
                loadStreetTable(dw.Row, true, streetTable);
            }
        }

        private void btnClearRegion_Click(object sender, EventArgs e)
        {
            cbDistrict.Enabled = false;
            cbCity.Enabled = false;
            cbSettlement.Enabled = false;
            cbStreet.Enabled = false;

            cbRegion.SelectedIndex = -1;
            cbDistrict.SelectedIndex = -1;
            cbCity.SelectedIndex = -1;
            cbSettlement.SelectedIndex = -1;
            cbStreet.SelectedIndex = -1;

            btnClearDistrict.Enabled = false;
            btnClearCity.Enabled = false;
            btnClearSettlement.Enabled = false;
            btnClearStreet.Enabled = false;

            level = "";
            streetAllow = false;
        }

        private void btnClearDistrict_Click(object sender, EventArgs e)
        {
            //загружаем список городов, не относящихся к районам            
            cityTable = readDBF("select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
            " AND mid(CODE, 3, 3) = '000' AND mid(CODE, 9, 3) = '000' AND mid(CODE, 6, 3) <> '000' and mid(CODE, 12, 2) = '00'");
            if (cityTable.Rows.Count != 0)
            {
                cbCity.DataSource = cityTable.DefaultView;
                cbCity.DisplayMember = "FULLNAME";
                cbCity.ValueMember = "CODE";
                cbCity.BindingContext = this.BindingContext;
                cbCity.SelectedIndex = -1;

                cbCity.Enabled = true;
                btnClearCity.Enabled = true;
            }
            else
            {
                cbCity.Enabled = false;
                btnClearCity.Enabled = false;
            }
            cbSettlement.Enabled = false;
            cbStreet.Enabled = false;

            cbDistrict.SelectedIndex = -1;
            cbCity.SelectedIndex = -1;
            cbSettlement.SelectedIndex = -1;
            cbStreet.SelectedIndex = -1;

            btnClearSettlement.Enabled = false;
            btnClearStreet.Enabled = false;

            level = "region";
            streetAllow = false;
        }

        private void btnClearCity_Click(object sender, EventArgs e)
        {
            if (cbCity.SelectedIndex != -1)
            {
                settlementTable.Clear();
                settlementTable = readDBF("select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                    " AND mid(CODE, 3, 3) = '" + district + "'AND mid(CODE, 6, 3) = '000' AND mid(CODE, 9, 3) <> '000' and mid(CODE, 12, 2) = '00'");
                if (settlementTable.Rows.Count != 0)
                {
                    cbSettlement.DataSource = settlementTable.DefaultView;
                    cbSettlement.DisplayMember = "FULLNAME";
                    cbSettlement.ValueMember = "CODE";
                    cbSettlement.BindingContext = this.BindingContext;
                    cbSettlement.SelectedIndex = -1;
                    cbSettlement.Enabled = true;
                    btnClearSettlement.Enabled = true;
                }
                else
                {
                    cbSettlement.Enabled = false;
                    btnClearSettlement.Enabled = false;
                    cbSettlement.SelectedIndex = -1;
                }

                cbDistrict.Enabled = true;
                btnClearDistrict.Enabled = true;
                cbStreet.Enabled = false;

                cbCity.SelectedIndex = -1;
                cbStreet.SelectedIndex = -1;

                btnClearStreet.Enabled = false;

                level = "district";
                streetAllow = false;
            }
        }

        private void btnClearSettlement_Click(object sender, EventArgs e)
        {
            if (cbSettlement.SelectedIndex != -1)
            {
                DataRowView dw = (DataRowView)cbSettlement.SelectedItem;
                loadStreetTable(dw.Row, true, streetTable);

                if (streetTable.Rows.Count != 0)
                {
                    cbStreet.Enabled = false;
                    btnClearStreet.Enabled = false;
                }
                else
                {
                    cbStreet.Enabled = false;
                    btnClearStreet.Enabled = false;
                }

                if (cbCity.Items.Count != 0)
                {
                    cbCity.Enabled = true;
                    btnClearCity.Enabled = true;
                }

                cbSettlement.SelectedIndex = -1;
                cbStreet.SelectedIndex = -1;
                if (cbCity.SelectedIndex != -1)
                    level = "city";
                else
                    level = "district";
            }
        }

        private void btnClearStreet_Click(object sender, EventArgs e)
        {
            cbStreet.SelectedIndex = -1;
            if (cbSettlement.SelectedIndex == -1)
                level = "city";
            else
                level = "settlement";
        }

        private void comboBox1_Click(object sender, EventArgs e)
        {
        }


        private void cbxLoadStreet_Click(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if (ofSTREET.FileName == "")
            {
                DialogResult res = ofSTREET.ShowDialog();
                if (res == DialogResult.OK)
                {
                    //var words = ofSTREET.FileName.Split('\\');
                    //for (var i = 0; i < words.Length - 1; i++)
                    //{
                    //    StreetFileName += words[i] + "\\";
                    //}

                    streetAllow = true;
                    if (level == "settlement" || level == "city" || level == "region")
                    {
                        if (level == "city")
                        {
                            var dw = (DataRowView)cbCity.SelectedItem;
                            loadStreetTable(dw.Row, true, streetTable);
                        }
                        else if (level == "settlement")
                        {
                            var dw = (DataRowView)cbSettlement.SelectedItem;
                            loadStreetTable(dw.Row, true, streetTable);
                        }
                        else if (level == "region")
                        {
                            var dw = (DataRowView)cbRegion.SelectedItem;
                            loadStreetTable(dw.Row, true, streetTable);
                        }

                    }
                }
            }
            else
            {
                if (!cb.Checked)
                {
                    cbStreet.Enabled = false;
                    btnClearStreet.Enabled = false;
                }
                else if (level == "settlement" || level == "city" || level == "region")
                {
                    if (level == "city")
                    {
                        var dw = (DataRowView)cbCity.SelectedItem;
                        loadStreetTable(dw.Row, true, streetTable);
                    }
                    else if (level == "settlement")
                    {
                        var dw = (DataRowView)cbSettlement.SelectedItem;
                        loadStreetTable(dw.Row, true, streetTable);
                    }
                    else if (level == "region")
                    {
                        var dw = (DataRowView)cbRegion.SelectedItem;
                        loadStreetTable(dw.Row, true, streetTable);
                    }
                }
            }
        }
             
        // Выбрана улица
        private void cbStreet_SelectionChangeCommitted(object sender, EventArgs e)
        {
            level = "street";
        }

        private decimal SaveRegion(DataRowView selectedRow, IfxConnection conn, IfxTransaction transactionID)
        {
            decimal nzp_stat;

            #region сохранение в верхний банк
            string selectedCode = selectedRow.Row["CODE"].ToString();
            string selectedFullname = selectedRow.Row["FULLNAME"].ToString().ToUpper();
            string sqlString = "SELECT * FROM " + upper_bank + "_data:s_stat where soato = '" + selectedCode + "'";
            IfxCommand ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
            IfxDataReader reader = ifxCommand.ExecuteReader();
            ifxCommand.Dispose();
            if (reader.HasRows)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data:s_stat SET ( stat, stat_t ) = ( '" + selectedFullname + "' , '" + selectedFullname + "' ) where soato = '" + selectedCode + "'";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                ifxCommand.ExecuteNonQuery();
                ifxCommand.Dispose();
                sqlString = "SELECT nzp_stat FROM " + upper_bank + "_data:s_stat WHERE soato = '" + selectedCode + "'";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                nzp_stat = Convert.ToDecimal(ifxCommand.ExecuteScalar());
                ifxCommand.Dispose();
            }
            else
            {
                //добавить
                sqlString = "INSERT INTO " + upper_bank + "_data:s_stat ( stat, stat_t, nzp_land, soato ) VALUES ( '" + selectedFullname + "', '" + selectedFullname + "' , '1' , '" + selectedCode + "')";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                ifxCommand.ExecuteNonQuery();
                ifxCommand.Dispose();
                nzp_stat = ClassDBUtils.GetSerialKey(conn, transactionID);
            }
            reader.Dispose();
            #endregion

            #region сохранение в нижние банки
            foreach (var bank in bottom_banks)
            {
                //добавить
                sqlString = "SELECT * FROM " + bank + "_data:s_stat where soato = '" + selectedCode + "'";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                reader = ifxCommand.ExecuteReader();
                ifxCommand.Dispose();
                if (!reader.HasRows)
                {
                    sqlString = "INSERT INTO " + bank + "_data:s_stat ( stat, stat_t, nzp_land, soato, nzp_stat ) VALUES " +
                                " ( '" + selectedFullname + "', '" + selectedFullname + "' , '1' , '" + selectedCode + "', '" + nzp_stat + "')";
                    ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                    ifxCommand.ExecuteNonQuery();
                    ifxCommand.Dispose();
                }
                else
                {
                    //обновить
                    sqlString = "UPDATE " + bank + "_data:s_stat SET ( stat, stat_t ) = ( '" + selectedFullname + "' , '" + selectedFullname + "' ) where soato = '" + selectedCode + "'";
                    ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                    ifxCommand.ExecuteNonQuery();
                    ifxCommand.Dispose();
                }
            }
            #endregion

            return nzp_stat;
        }

        private decimal SaveDistricrt(DataRow selectedRow, IfxConnection conn, IfxTransaction transactionID, decimal nzp_stat)
        {
            decimal nzp_town;

            #region сохранение в верхний банк
            string selectedCode = selectedRow["CODE"].ToString();
            string selectedFullname = selectedRow["FULLNAME"].ToString().ToUpper();
            string sqlString = "SELECT * FROM " + upper_bank + "_data:s_town where soato = '" + selectedCode + "'";
            IfxCommand ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
            IfxDataReader reader = ifxCommand.ExecuteReader();
            ifxCommand.Dispose();
            if (reader.HasRows)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data:s_town SET ( town, town_t, nzp_stat ) = ( '" + selectedFullname + "' , '" + selectedFullname + "', '" + nzp_stat.ToString() + "' ) where soato = '" + selectedCode + "'";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                ifxCommand.ExecuteNonQuery();
                ifxCommand.Dispose();
                sqlString = "SELECT nzp_town FROM " + upper_bank + "_data:s_town WHERE soato = '" + selectedCode + "'";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                nzp_town = Convert.ToDecimal(ifxCommand.ExecuteScalar());
                ifxCommand.Dispose();
            }
            else
            {
                //добавить
                sqlString = "INSERT INTO " + upper_bank + "_data:s_town ( town, town_t, nzp_stat, soato ) VALUES ( '" + selectedFullname + "', '" + selectedFullname + "' , '" + nzp_stat.ToString() + "' , '" + selectedCode + "')";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                ifxCommand.ExecuteNonQuery();
                ifxCommand.Dispose();
                nzp_town = ClassDBUtils.GetSerialKey(conn, transactionID);
            }
            reader.Dispose();
            #endregion

            #region сохранение в нижние банки
            foreach (var bank in bottom_banks)
            {
                //добавить
                sqlString = "SELECT * FROM " + bank + "_data:s_town where soato = '" + selectedCode + "'";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                reader = ifxCommand.ExecuteReader();
                ifxCommand.Dispose();
                if (!reader.HasRows)
                {
                    sqlString = "INSERT INTO " + bank + "_data:s_town ( town, town_t, nzp_stat, soato, nzp_town ) VALUES " +
                                " ( '" + selectedFullname + "', '" + selectedFullname + "' , '1' , '" + selectedCode + "', '" + nzp_town + "')";
                    ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                    ifxCommand.ExecuteNonQuery();
                    ifxCommand.Dispose();
                }
                else
                {
                    //обновить
                    sqlString = "UPDATE " + bank + "_data:s_town SET ( town, town_t ) = ( '" + selectedFullname + "' , '" + selectedFullname + "' ) where soato = '" + selectedCode + "'";
                    ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                    ifxCommand.ExecuteNonQuery();
                    ifxCommand.Dispose();
                }
            }
            #endregion

            return nzp_town;
        }

        private decimal SaveCity(DataRow selectedRow, IfxConnection conn, IfxTransaction transactionID, decimal nzp_stat)
        {
            decimal nzp_town;

            #region сохранение в верхний банк
            string selectedCode = selectedRow["CODE"].ToString();
            string selectedFullname = selectedRow["FULLNAME"].ToString().ToUpper();
            string sqlString = "SELECT * FROM " + upper_bank + "_data:s_town where soato = '" + selectedCode + "'";
            IfxCommand ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
            IfxDataReader reader = ifxCommand.ExecuteReader();
            ifxCommand.Dispose();
            if (reader.HasRows)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data:s_town SET ( town, town_t, nzp_stat ) = " +
                            " ( '" + selectedFullname + "' , '" + selectedFullname + "', '" + nzp_stat.ToString() + "' ) where soato = '" + selectedCode + "'";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                ifxCommand.ExecuteNonQuery();
                ifxCommand.Dispose();
                sqlString = "SELECT nzp_town FROM " + upper_bank + "_data:s_town WHERE soato = '" + selectedCode + "'";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                nzp_town = Convert.ToDecimal(ifxCommand.ExecuteScalar());
                ifxCommand.Dispose();
            }
            else
            {
                //добавить
                sqlString = "INSERT INTO " + upper_bank + "_data:s_town ( town, town_t, nzp_stat, soato ) VALUES " +
                            " ( '" + selectedFullname + "', '" + selectedFullname + "' , '" + nzp_stat.ToString() + "' , '" + selectedCode + "')";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                ifxCommand.ExecuteNonQuery();
                ifxCommand.Dispose();
                nzp_town = ClassDBUtils.GetSerialKey(conn, transactionID);
            }
            reader.Dispose();
            #endregion

            #region сохранение в нижние банки
            foreach (var bank in bottom_banks)
            {
                //добавить
                sqlString = "SELECT * FROM " + bank + "_data:s_town where soato = '" + selectedCode + "'";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                reader = ifxCommand.ExecuteReader();
                ifxCommand.Dispose();
                if (!reader.HasRows)
                {
                    sqlString = "INSERT INTO " + bank + "_data:s_town ( town, town_t, nzp_stat, soato, nzp_town ) VALUES " +
                                " ( '" + selectedFullname + "', '" + selectedFullname + "' , '1' , '" + selectedCode + "', '" + nzp_town + "')";
                    ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                    ifxCommand.ExecuteNonQuery();
                    ifxCommand.Dispose();
                }
                else
                {
                    //обновить
                    sqlString = "UPDATE " + bank + "_data:s_town SET ( town, town_t ) = ( '" + selectedFullname + "' , '" + selectedFullname + "' ) where soato = '" + selectedCode + "'";
                    ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                    ifxCommand.ExecuteNonQuery();
                    ifxCommand.Dispose();
                }
            }
            #endregion

            return nzp_town;
        }

        private decimal SaveSettlement(DataRow selectedRow, IfxConnection conn, IfxTransaction transactionID, decimal nzp_town)
        {
            decimal nzp_raj;

            #region сохранение в верхний банк
            string selectedCode = selectedRow["CODE"].ToString();
            string selectedFullname = selectedRow["FULLNAME"].ToString().ToUpper();
            string sqlString = "SELECT * FROM " + upper_bank + "_data:s_rajon where soato = '" + selectedCode + "' and nzp_town = " + nzp_town.ToString();
            IfxCommand ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
            IfxDataReader reader = ifxCommand.ExecuteReader();
            ifxCommand.Dispose();
            if (reader.HasRows)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data:s_rajon SET ( rajon, rajon_t ) = ( '" + selectedFullname + "', '" + selectedFullname + "' ) where soato = '" + selectedCode + "' and nzp_town = " + nzp_town.ToString();
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                ifxCommand.ExecuteNonQuery();
                ifxCommand.Dispose();
                sqlString = "SELECT nzp_raj FROM " + upper_bank + "_data:s_rajon WHERE soato = '" + selectedCode + "' and nzp_town = " + nzp_town.ToString();
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                nzp_raj = Convert.ToDecimal(ifxCommand.ExecuteScalar());
                ifxCommand.Dispose();
            }
            else
            {
                //добавить                
                sqlString = "INSERT INTO " + upper_bank + "_data:s_rajon ( nzp_town, rajon, rajon_t, soato ) VALUES (  '" + nzp_town.ToString() + "' ,'" + selectedFullname + "', '" + selectedFullname + "' , '" + selectedCode + "')";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                ifxCommand.ExecuteNonQuery();
                ifxCommand.Dispose();
                nzp_raj = ClassDBUtils.GetSerialKey(conn, transactionID);
            }
            reader.Dispose();
            #endregion

            #region сохранение в нижние банки
            foreach (var bank in bottom_banks)
            {
                //добавить
                sqlString = "SELECT * FROM " + bank + "_data:s_rajon where soato = '" + selectedCode + "'";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                reader = ifxCommand.ExecuteReader();
                ifxCommand.Dispose();
                if (!reader.HasRows)
                {
                    sqlString = "INSERT INTO " + bank + "_data:s_rajon ( rajon, rajon_t, nzp_town, soato, nzp_raj ) VALUES " +
                                " ( '" + selectedFullname + "', '" + selectedFullname + "' , '1' , '" + selectedCode + "', '" + nzp_raj + "')";
                    ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                    ifxCommand.ExecuteNonQuery();
                    ifxCommand.Dispose();
                }
                else
                {
                    //обновить
                    sqlString = "UPDATE " + bank + "_data:s_rajon SET ( rajon, rajon_t ) = ( '" + selectedFullname + "' , '" + selectedFullname + "' ) where soato = '" + selectedCode + "'";
                    ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                    ifxCommand.ExecuteNonQuery();
                    ifxCommand.Dispose();
                }
            }
            #endregion

            return nzp_raj;
        }

        private void SaveStreet(DataRow selectedRow, IfxConnection conn, IfxTransaction transactionID, decimal nzp_raj)
        {
            decimal nzp_ul;

            #region сохранение в верхний банк
            string selectedCode = selectedRow["CODE"].ToString();
            string selectedFullname = selectedRow["FULLNAME"].ToString().ToUpper();
            string selectedName = selectedRow["NAME"].ToString().ToUpper();
            string selectedSocr = selectedRow["SOCR"].ToString().ToUpper();
            string sqlString = "SELECT * FROM " + upper_bank + "_data:s_ulica where soato = '" + selectedCode + "'";
            IfxCommand ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
            IfxDataReader reader = ifxCommand.ExecuteReader();
            ifxCommand.Dispose();
            if (reader.HasRows)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data:s_ulica SET ( ulica, ulicareg, nzp_raj ) = ( '" + selectedName + "' , '" + selectedSocr + "', '" + nzp_raj + "') where soato = '" + selectedCode + "'";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                ifxCommand.ExecuteNonQuery();
                ifxCommand.Dispose();
                sqlString = "SELECT nzp_ul FROM " + upper_bank + "_data:s_ulica WHERE soato = '" + selectedCode + "' and nzp_raj = " + nzp_raj.ToString();
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                nzp_ul = Convert.ToDecimal(ifxCommand.ExecuteScalar());
                ifxCommand.Dispose();
            }
            else
            {
                //добавить
                sqlString = "INSERT INTO " + upper_bank + "_data:s_ulica ( ulica, nzp_raj, soato, ulicareg ) VALUES ( '" + selectedName + "', '" + nzp_raj.ToString() + "' , '" + selectedCode + "' , '" + selectedSocr + "')";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                ifxCommand.ExecuteNonQuery();
                ifxCommand.Dispose();
                nzp_ul = ClassDBUtils.GetSerialKey(conn, transactionID);
            }
            reader.Dispose();
            #endregion

            #region сохранение в нижние банки
            foreach (var bank in bottom_banks)
            {
                //добавить
                sqlString = "SELECT * FROM " + bank + "_data:s_ulica where soato = '" + selectedCode + "'";
                ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                reader = ifxCommand.ExecuteReader();
                ifxCommand.Dispose();
                if (!reader.HasRows)
                {
                    sqlString = "INSERT INTO " + bank + "_data:s_ulica ( ulica, ulicareg, nzp_raj, soato, nzp_ul ) VALUES " +
                                " ( '" + selectedName + "', '" + selectedSocr + "' , '1' , '" + selectedCode + "', '" + nzp_ul + "')";
                    ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                    ifxCommand.ExecuteNonQuery();
                    ifxCommand.Dispose();
                }
                else
                {
                    //обновить
                    sqlString = "UPDATE " + bank + "_data:s_ulica SET ( ulica, ulicareg ) = ( '" + selectedName + "' , '" + selectedSocr + "' ) where soato = '" + selectedCode + "'";
                    ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                    ifxCommand.ExecuteNonQuery();
                    ifxCommand.Dispose();
                }
            }
            #endregion
        }

        // Выгрузка в базу
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (level == "")
            {
                MessageBox.Show("Выберите субъект.");
            }
            else
            {
                loadForm = new Loading.Loading("Выполняется выгрузка");
                thread = new Thread(
                    () =>
                    {
                        loadForm.ShowDialog();
                    });
                thread.Start();

                //соединение с БД
                IfxConnection conn = new IfxConnection();
                conn.ConnectionString = ConnectionString;

                SetProgressBar(loadForm, 5);

                string sqlString = "";
                IfxCommand ifxCommand = new IfxCommand(sqlString, conn);
                conn.Open();

                IfxTransaction transactionID = conn.BeginTransaction();
                ifxCommand.Transaction = transactionID;
                try
                {
                    SetProgressBar(loadForm, 10);
                    decimal nzp_city = 0;
                    decimal nzp_raj = 0;
                    decimal nzp_town = 0;

                    #region очистка адресного пространства
                    if (cbClearAddressSpace.Checked)
                    {
                        sqlString = " delete from " + upper_bank + "_data:s_stat; delete from " + upper_bank + "_data:s_town; delete from " + upper_bank + "_data:s_rajon; " +
                                    " delete from " + upper_bank + "_data:s_ulica; delete from " + upper_bank + "_data:dom; delete from " + upper_bank + "_data:kvar;";
                        ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                        ifxCommand.ExecuteNonQuery();
                        ifxCommand.Dispose();
                        foreach (var bank in bottom_banks)
                        {
                            sqlString = " delete from " + bank + "_data:s_stat; delete from " + bank + "_data:s_town; delete from " + bank + "_data:s_rajon; " +
                                        " delete from " + bank + "_data:s_ulica; delete from " + bank + "_data:dom; delete from " + bank + "_data:kvar;";
                            ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                            ifxCommand.ExecuteNonQuery();
                            ifxCommand.Dispose();
                        }
                    }
                    #endregion

                    #region Для выгрузки выбрана улица
                    if (level == "street")
                    {
                        DataRowView selectedRow = (DataRowView)cbRegion.SelectedItem;
                        decimal nzp_stat = SaveRegion(selectedRow, conn, transactionID);

                        if (cbDistrict.SelectedIndex != -1)
                        {
                            selectedRow = (DataRowView)cbDistrict.SelectedItem;
                            nzp_town = SaveDistricrt(selectedRow.Row, conn, transactionID, nzp_stat);
                        }
                        else
                        {
                            DataTable tempdt = new DataTable();
                            tempdt.Columns.Add("FULLNAME", typeof(String));
                            tempdt.Columns.Add("CODE", typeof(String));
                            tempdt.Rows.Add();
                            DataRow drv = tempdt.Rows[0];
                            drv["CODE"] = "-";
                            drv["FULLNAME"] = "-";
                            nzp_town = SaveDistricrt(drv, conn, transactionID, nzp_stat);
                        }


                        if (cbCity.SelectedIndex != -1)
                        {
                            selectedRow = (DataRowView)cbCity.SelectedItem;
                            nzp_city = SaveCity(selectedRow.Row, conn, transactionID, nzp_stat);
                        }

                        SetProgressBar(loadForm, 50);

                        if (cbSettlement.SelectedIndex != -1)
                        {
                            selectedRow = (DataRowView)cbSettlement.SelectedItem;
                            if (nzp_city != 0)
                                if (!cbxIgnoreCityDistrict.Checked)
                                    nzp_raj = SaveSettlement(selectedRow.Row, conn, transactionID, nzp_city);
                            if (nzp_city == 0)
                                nzp_raj = SaveSettlement(selectedRow.Row, conn, transactionID, nzp_town);
                        }

                        SetProgressBar(loadForm, 99);

                        selectedRow = (DataRowView)cbStreet.SelectedItem;
                        if (nzp_raj == 0)
                        {
                            DataTable tempdt = new DataTable();
                            tempdt.Columns.Add("FULLNAME", typeof(String));
                            tempdt.Columns.Add("CODE", typeof(String));
                            tempdt.Rows.Add();
                            DataRow drv = tempdt.Rows[0];
                            drv["CODE"] = "-";
                            drv["FULLNAME"] = "-";
                            if (nzp_city != 0)
                                nzp_raj = SaveSettlement(drv, conn, transactionID, nzp_city);
                            else
                                nzp_raj = SaveSettlement(drv, conn, transactionID, nzp_town);

                            SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                        }
                        else
                            SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);

                    }
                    #endregion Для выгрузки выбрана улица

                    #region Для выгрузки выбран населенный пункт
                    if (level == "settlement") 
                    {
                        DataRowView selectedRow = (DataRowView)cbRegion.SelectedItem;
                        decimal nzp_stat = SaveRegion(selectedRow, conn, transactionID);

                        if (cbDistrict.SelectedIndex != -1)
                        {
                            selectedRow = (DataRowView)cbDistrict.SelectedItem;
                            nzp_town = SaveDistricrt(selectedRow.Row, conn, transactionID, nzp_stat);
                        }

                        if (cbCity.SelectedIndex != -1)
                        {
                            selectedRow = (DataRowView)cbCity.SelectedItem;
                            nzp_city = SaveCity(selectedRow.Row, conn, transactionID, nzp_stat);
                        }

                        SetProgressBar(loadForm, 70);

                        selectedRow = (DataRowView)cbSettlement.SelectedItem;
                        if (nzp_city != 0)
                            if (!cbxIgnoreCityDistrict.Checked)
                                nzp_raj = SaveSettlement(selectedRow.Row, conn, transactionID, nzp_city);
                        if (nzp_city == 0)
                            nzp_raj = SaveSettlement(selectedRow.Row, conn, transactionID, nzp_town);

                        if (cbxLoadStreet.Checked)
                        {
                            for (int i = 0; i < cbStreet.Items.Count; i++)
                            {
                                selectedRow = (DataRowView)cbStreet.Items[i];
                                if (nzp_raj == 0)
                                {
                                    DataTable tempdt = new DataTable();
                                    tempdt.Columns.Add("FULLNAME", typeof(String));
                                    tempdt.Columns.Add("CODE", typeof(String));
                                    tempdt.Rows.Add();
                                    DataRow drv = tempdt.Rows[0];
                                    drv["CODE"] = "-";
                                    drv["FULLNAME"] = "-";
                                    if (nzp_city != 0)
                                        nzp_raj = SaveSettlement(drv, conn, transactionID, nzp_city);
                                    else
                                        nzp_raj = SaveSettlement(drv, conn, transactionID, nzp_town);

                                    SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                                }
                                else
                                    SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                            }
                        }

                        SetProgressBar(loadForm, 99);
                    }
                    #endregion Для выгрузки выбран населенный пункт

                    #region Для выгрузки выбран город
                    if (level == "city") 
                    {
                        DataRowView selectedRow = (DataRowView)cbRegion.SelectedItem;
                        DataTable tempStreetTable = new DataTable();
                        tempStreetTable.Columns.Add("FULLNAME", typeof(String));
                        decimal nzp_stat = SaveRegion(selectedRow, conn, transactionID);

                        if (cbDistrict.SelectedIndex != -1)
                        {
                            selectedRow = (DataRowView)cbDistrict.SelectedItem;
                            nzp_town = SaveDistricrt(selectedRow.Row, conn, transactionID, nzp_stat);
                        }

                        selectedRow = (DataRowView)cbCity.SelectedItem;
                        nzp_city = SaveCity(selectedRow.Row, conn, transactionID, nzp_stat);

                        SetProgressBar(loadForm, 30);

                        DataTable tempdt = new DataTable();
                        tempdt.Columns.Add("FULLNAME", typeof(String));
                        tempdt.Columns.Add("CODE", typeof(String));
                        tempdt.Rows.Add();
                        DataRow drv = tempdt.Rows[0];
                        drv["CODE"] = "-";
                        drv["FULLNAME"] = "-";
                        nzp_raj = SaveSettlement(drv, conn, transactionID, nzp_city);

                        if (cbxLoadStreet.Checked)
                        {
                            for (int i = 0; i < cbStreet.Items.Count; i++)
                            {
                                if (i == Convert.ToInt32(cbStreet.Items.Count * 0.2))
                                    SetProgressBar(loadForm, 40);
                                if (i == Convert.ToInt32(cbStreet.Items.Count * 0.4))
                                    SetProgressBar(loadForm, 50);
                                if (i == Convert.ToInt32(cbStreet.Items.Count * 0.6))
                                    SetProgressBar(loadForm, 60);
                                if (i == Convert.ToInt32(cbStreet.Items.Count * 0.8))
                                    SetProgressBar(loadForm, 80);

                                selectedRow = (DataRowView)cbStreet.Items[i];
                                SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                            }
                        }

                        if (cbSettlement.Items.Count != 0)
                        {
                            for (int j = 0; j < cbSettlement.Items.Count; j++)
                            {
                                selectedRow = (DataRowView)cbSettlement.Items[j];
                                if (!cbxIgnoreCityDistrict.Checked)
                                {
                                    nzp_raj = SaveSettlement(selectedRow.Row, conn, transactionID, nzp_city);
                                }

                                if (cbxLoadStreet.Checked)
                                {
                                    loadStreetTable(selectedRow.Row, false, tempStreetTable);
                                    for (int i = 0; i < tempStreetTable.Rows.Count; i++)
                                    {
                                        selectedRow = tempStreetTable.DefaultView[i];
                                        SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                                    }
                                }
                            }
                        }
                    }
                    #endregion Для выгрузки выбран город

                    #region Для выгрузки выбран район
                    if (level == "district")
                    {
                        DataRowView selectedRow = (DataRowView)cbRegion.SelectedItem;
                        decimal nzp_stat = SaveRegion(selectedRow, conn, transactionID);

                        DataTable tempSettlementTable = new DataTable();
                        DataTable tempStreetTable = new DataTable();
                        tempStreetTable.Columns.Add("FULLNAME", typeof(String));

                        selectedRow = (DataRowView)cbDistrict.SelectedItem;
                        nzp_town = SaveDistricrt(selectedRow.Row, conn, transactionID, nzp_stat);

                        SetProgressBar(loadForm, 20);

                        for (int c = 0; c < cbCity.Items.Count; c++)
                        {
                            selectedRow = (DataRowView)cbCity.Items[c];
                            nzp_city = SaveCity(selectedRow.Row, conn, transactionID, nzp_stat);
                            city = selectedRow["CODE"].ToString().Substring(5, 3);

                            tempSettlementTable.Rows.Clear();
                            tempSettlementTable = readDBF("select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                                " AND mid(CODE, 3, 3) = '" + district + "'AND mid(CODE, 6, 3) = '" + city + "' AND mid(CODE, 9, 3) <> '000' and mid(CODE, 12, 2) = '00'");

                            DataTable tempdt = new DataTable();
                            tempdt.Columns.Add("FULLNAME", typeof(String));
                            tempdt.Columns.Add("CODE", typeof(String));
                            tempdt.Rows.Add();
                            DataRow drv = tempdt.Rows[0];
                            drv["CODE"] = "-";
                            drv["FULLNAME"] = "-";
                            nzp_raj = SaveSettlement(drv, conn, transactionID, nzp_city);

                            if (cbxLoadStreet.Checked)
                            {
                                tempStreetTable.Rows.Clear();
                                loadStreetTable(selectedRow.Row, true, tempStreetTable);
                                for (int i = 0; i < tempStreetTable.Rows.Count; i++)
                                {
                                    selectedRow = tempStreetTable.DefaultView[i];
                                    SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                                }
                            }

                            if (c == Convert.ToInt32(cbCity.Items.Count * 0.2))
                                SetProgressBar(loadForm, 40);
                            if (c == Convert.ToInt32(cbCity.Items.Count * 0.4))
                                SetProgressBar(loadForm, 50);
                            if (c == Convert.ToInt32(cbCity.Items.Count * 0.6))
                                SetProgressBar(loadForm, 60);
                            if (c == Convert.ToInt32(cbCity.Items.Count * 0.8))
                                SetProgressBar(loadForm, 80);

                            if (tempSettlementTable.Rows.Count != 0)
                            {
                                for (int s = 0; s < tempSettlementTable.Rows.Count; s++)
                                {
                                    selectedRow = tempSettlementTable.DefaultView[s];
                                    if (!cbxIgnoreCityDistrict.Checked)
                                    {
                                        nzp_raj = SaveSettlement(selectedRow.Row, conn, transactionID, nzp_city);
                                    }
                                    if (cbxLoadStreet.Checked)
                                    {
                                        loadStreetTable(tempSettlementTable.Rows[s], false, tempStreetTable);
                                        for (int i = 0; i < tempStreetTable.Rows.Count; i++)
                                        {
                                            selectedRow = tempStreetTable.DefaultView[i];
                                            SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                                        }
                                    }
                                }
                            }
                        }
                        tempSettlementTable.Clear();
                        tempSettlementTable = readDBF("select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                            " AND mid(CODE, 3, 3) = '" + district + "'AND mid(CODE, 6, 3) = '000' AND mid(CODE, 9, 3) <> '000' and mid(CODE, 12, 2) = '00'");
                        for (int j = 0; j < tempSettlementTable.Rows.Count; j++)
                        {
                            selectedRow = tempSettlementTable.DefaultView[j];
                            nzp_raj = SaveSettlement(selectedRow.Row, conn, transactionID, nzp_town);

                            if (j == tempSettlementTable.Rows.Count / 2)
                                SetProgressBar(loadForm, 90);

                            if (cbxLoadStreet.Checked)
                            {
                                loadStreetTable(tempSettlementTable.Rows[j], false, tempStreetTable);
                                for (int i = 0; i < tempStreetTable.Rows.Count; i++)
                                {
                                    selectedRow = tempStreetTable.DefaultView[i];
                                    SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                                }
                            }
                        }
                    }
                    #endregion Для выгрузки выбран район

                    #region Для выгрузки выбран регион
                    if (level == "region")
                    {
                        DataRowView selectedRow = (DataRowView)cbRegion.SelectedItem;
                        decimal nzp_stat = SaveRegion(selectedRow, conn, transactionID);

                        DataTable tempSettlementTable = new DataTable();
                        DataTable tempCityTable = new DataTable();
                        DataTable tempStreetTable = new DataTable();
                        tempStreetTable.Columns.Add("FULLNAME", typeof(String));

                        SetProgressBar(loadForm, 20);

                        #region сохранение улиц, принадлежащих региону
                        if (cbxLoadStreet.Checked && cbStreet.Items.Count != 0)
                        {
                            DataTable tempdt = new DataTable();
                            tempdt.Columns.Add("FULLNAME", typeof(String));
                            tempdt.Columns.Add("CODE", typeof(String));
                            tempdt.Rows.Add();
                            DataRow drv = tempdt.Rows[0];
                            drv["CODE"] = "-";
                            drv["FULLNAME"] = "-";

                            nzp_town = SaveDistricrt(drv, conn, transactionID, nzp_stat);
                            nzp_raj = SaveSettlement(drv, conn, transactionID, nzp_town);

                            for (int f = 0; f < cbStreet.Items.Count; f++)
                            {
                                selectedRow = (DataRowView)cbStreet.Items[f];
                                SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                            }
                        }
                        #endregion сохранение улиц, принадлежащих региону

                        for (int c = 0; c < cbCity.Items.Count; c++)
                        {
                            selectedRow = (DataRowView)cbCity.Items[c];
                            nzp_city = SaveCity(selectedRow.Row, conn, transactionID, nzp_stat);
                            city = selectedRow["CODE"].ToString().Substring(5, 3);

                            tempSettlementTable.Rows.Clear();
                            tempSettlementTable = readDBF("select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                                " AND mid(CODE, 3, 3) = '000'AND mid(CODE, 6, 3) = '" + city + "' AND mid(CODE, 9, 3) <> '000' and mid(CODE, 12, 2) = '00'");

                            DataTable tempdt = new DataTable();
                            tempdt.Columns.Add("FULLNAME", typeof(String));
                            tempdt.Columns.Add("CODE", typeof(String));
                            tempdt.Rows.Add();
                            DataRow drv = tempdt.Rows[0];
                            drv["CODE"] = "-";
                            drv["FULLNAME"] = "-";
                            nzp_raj = SaveSettlement(drv, conn, transactionID, nzp_city);

                            if (cbxLoadStreet.Checked)
                            {
                                tempStreetTable.Rows.Clear();
                                loadStreetTable(selectedRow.Row, true, tempStreetTable);
                                for (int i = 0; i < tempStreetTable.Rows.Count; i++)
                                {
                                    selectedRow = tempStreetTable.DefaultView[i];
                                    SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                                }
                            }

                            if (tempSettlementTable.Rows.Count != 0)
                            {
                                for (int s = 0; s < tempSettlementTable.Rows.Count; s++)
                                {
                                    selectedRow = tempSettlementTable.DefaultView[s];
                                    if (!cbxIgnoreCityDistrict.Checked)
                                    {
                                        nzp_raj = SaveSettlement(selectedRow.Row, conn, transactionID, nzp_city);
                                    }
                                    if (cbxLoadStreet.Checked)
                                    {
                                        loadStreetTable(tempSettlementTable.Rows[s], false, tempStreetTable);
                                        for (int i = 0; i < tempStreetTable.Rows.Count; i++)
                                        {
                                            selectedRow = tempStreetTable.DefaultView[i];
                                            SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                                        }
                                    }
                                }
                            }
                        }

                        for (int d = 0; d < districtTable.Rows.Count; d++)
                        {
                            selectedRow = districtTable.DefaultView[d];
                            nzp_town = SaveDistricrt(selectedRow.Row, conn, transactionID, nzp_stat);

                            district = districtTable.Rows[d]["CODE"].ToString().Substring(2, 3);
                            tempCityTable.Rows.Clear();
                            tempCityTable = readDBF("select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                                " AND mid(CODE, 3, 3) = '" + district + "'AND mid(CODE, 9, 3) = '000' AND mid(CODE, 6, 3) <> '000' and mid(CODE, 12, 2) = '00'");
                            if (d == Convert.ToInt32(districtTable.Rows.Count * 0.2))
                                SetProgressBar(loadForm, 40);
                            if (d == Convert.ToInt32(districtTable.Rows.Count * 0.4))
                                SetProgressBar(loadForm, 50);
                            if (d == Convert.ToInt32(districtTable.Rows.Count * 0.6))
                                SetProgressBar(loadForm, 60);
                            if (d == Convert.ToInt32(districtTable.Rows.Count * 0.8))
                                SetProgressBar(loadForm, 80);

                            for (int c = 0; c < tempCityTable.Rows.Count; c++)
                            {
                                selectedRow = tempCityTable.DefaultView[c];
                                nzp_city = SaveCity(selectedRow.Row, conn, transactionID, nzp_stat);
                                city = selectedRow["CODE"].ToString().Substring(5, 3);

                                DataTable tempdt = new DataTable();
                                tempdt.Columns.Add("FULLNAME", typeof(String));
                                tempdt.Columns.Add("CODE", typeof(String));
                                tempdt.Rows.Add();
                                DataRow drv = tempdt.Rows[0];
                                drv["CODE"] = "-";
                                drv["FULLNAME"] = "-";
                                nzp_raj = SaveSettlement(drv, conn, transactionID, nzp_city);

                                if (cbxLoadStreet.Checked)
                                {
                                    tempStreetTable.Rows.Clear();
                                    loadStreetTable(selectedRow.Row, true, tempStreetTable);
                                    for (int i = 0; i < tempStreetTable.Rows.Count; i++)
                                    {
                                        selectedRow = tempStreetTable.DefaultView[i];
                                        SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                                    }
                                }

                                tempSettlementTable.Rows.Clear();
                                tempSettlementTable = readDBF("select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                                    " AND mid(CODE, 3, 3) = '" + district + "'AND mid(CODE, 6, 3) = '" + city + "' AND mid(CODE, 9, 3) <> '000' and mid(CODE, 12, 2) = '00'");

                                if (tempSettlementTable.Rows.Count != 0)
                                {
                                    for (int s = 0; s < tempSettlementTable.Rows.Count; s++)
                                    {
                                        selectedRow = tempSettlementTable.DefaultView[s];
                                        if (!cbxIgnoreCityDistrict.Checked)
                                        {
                                            nzp_raj = SaveSettlement(selectedRow.Row, conn, transactionID, nzp_city);
                                        }
                                        if (cbxLoadStreet.Checked)
                                        {
                                            loadStreetTable(tempSettlementTable.Rows[s], false, tempStreetTable);
                                            for (int i = 0; i < tempStreetTable.Rows.Count; i++)
                                            {
                                                selectedRow = tempStreetTable.DefaultView[i];
                                                SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                                            }
                                        }
                                    }
                                }
                            }
                            tempSettlementTable.Clear();
                            tempSettlementTable = readDBF("select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                                " AND mid(CODE, 3, 3) = '" + district + "'AND mid(CODE, 6, 3) = '000' AND mid(CODE, 9, 3) <> '000' and mid(CODE, 12, 2) = '00'");
                            for (int j = 0; j < tempSettlementTable.Rows.Count; j++)
                            {
                                selectedRow = tempSettlementTable.DefaultView[j];
                                nzp_raj = SaveSettlement(selectedRow.Row, conn, transactionID, nzp_town);

                                if (cbxLoadStreet.Checked)
                                {
                                    loadStreetTable(tempSettlementTable.Rows[j], false, tempStreetTable);
                                    for (int i = 0; i < tempStreetTable.Rows.Count; i++)
                                    {
                                        selectedRow = tempStreetTable.DefaultView[i];
                                        SaveStreet(selectedRow.Row, conn, transactionID, nzp_raj);
                                    }
                                }
                            }
                        }
                    }
                    #endregion регион

                    transactionID.Commit();
                    conn.Close();

                    CloseProgressBar(loadForm);
                    thread.Abort();

                    MessageBox.Show("Выгрузка прошла успешно.");
                }
                catch (IfxException ex)
                {
                    transactionID.Rollback();
                    conn.Close();
                    CloseProgressBar(loadForm);
                    thread.Abort();
                    MessageBox.Show(ex.Message);
                }
            }
        }

        // Закрытие окна прогрессбара
        delegate void CloseProgressBarCallBack(Loading.Loading form);
        private void CloseProgressBar(Loading.Loading form)
        {
            if (form.InvokeRequired)
            {
                form.BeginInvoke(new CloseProgressBarCallBack(CloseProgressBar), form);
            }
            else
            {
                form.Close();
            }
        }

        // Установка значения прогрессбара
        delegate void SetProgressBarCallBack(Loading.Loading form, int val);
        private void SetProgressBar(Loading.Loading form, int val)
        {
            if (form.InvokeRequired)
            {
                form.BeginInvoke(new SetProgressBarCallBack(SetProgressBar), form, val);
            }
            else
            {
                form.SetValue(val);
            }
        }

        // Загрузка улиц из DBF
        private void loadStreetTable(DataRow dr, bool show, DataTable table)
        {
            #region vfpoledb
            //OleDbConnectionStringBuilder bldr = new OleDbConnectionStringBuilder();
            //bldr.DataSource = FileName;
            //bldr.Provider = "vfpoledb.1";
            //OleDbConnection oDbCon = new OleDbConnection(bldr.ConnectionString);
            #endregion
            OleDbConnection oDbCon = new OleDbConnection();
            var myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                            "Data Source=" + FilePath + ";Extended Properties=dBASE III;";
            oDbCon.ConnectionString = myConnectionString;
            oDbCon.Open();
            // Прочитать dbf-файл в DataTable
            try
            {
                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandText = "select * from street where mid(CODE, 1 , 11) = '" + dr["CODE"].ToString() + "' and mid(CODE, 16 , 2) = '00'";
                cmd.Connection = oDbCon;
                OleDbDataAdapter da = new OleDbDataAdapter();
                da.SelectCommand = cmd;
                table.Rows.Clear();
                da.Fill(table);
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    #region меняем кодировку с досовской
                    //table.Rows[i]["NAME"] = Encoding.GetEncoding(866).GetString(
                    //    Encoding.Default.GetBytes(table.Rows[i]["NAME"].ToString()));
                    //table.Rows[i]["SOCR"] = Encoding.GetEncoding(866).GetString(
                    //    Encoding.Default.GetBytes(table.Rows[i]["SOCR"].ToString()));
                    #endregion

                    table.Rows[i]["CODE"] = table.Rows[i]["CODE"].ToString().Substring(0, 15);
                    string name = "";
                    if (table.Rows[i]["NAME"] != null)
                    {
                        name = table.Rows[i]["NAME"].ToString().Trim();
                        table.Rows[i]["NAME"] = name;
                    }
                    string socr = "";
                    if (table.Rows[i]["SOCR"] != null)
                    {
                        socr = table.Rows[i]["SOCR"].ToString().Trim();
                        table.Rows[i]["SOCR"] = socr;
                    }

                    table.Rows[i]["FULLNAME"] = name + " " + socr;
                }
                if (table.Rows.Count != 0 && show)
                {
                    cbStreet.DataSource = table.DefaultView;
                    cbStreet.DisplayMember = "FULLNAME";
                    cbStreet.ValueMember = "CODE";
                    cbStreet.BindingContext = this.BindingContext;
                    cbStreet.SelectedIndex = -1;

                    cbStreet.Enabled = true;
                    btnClearStreet.Enabled = true;
                }
                else
                {
                    cbStreet.Enabled = false;
                    btnClearStreet.Enabled = false;
                    cbStreet.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        // Считать из DBF файла
        private DataTable readDBF(string sqlString)
        {
            // Создать объект подключения
            #region vfpoledb
            //OleDbConnectionStringBuilder bldr = new OleDbConnectionStringBuilder();
            //bldr.DataSource = FileName;
            //bldr.Provider = "vfpoledb.1";
            //OleDbConnection oDbCon = new OleDbConnection(bldr.ConnectionString);
            #endregion

            OleDbConnection oDbCon = new OleDbConnection();
            var myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                            "Data Source=" + FilePath + ";Extended Properties=dBASE III;";
            oDbCon.ConnectionString = myConnectionString;
            oDbCon.Open();

            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = sqlString;
            cmd.Connection = oDbCon;
            // Адаптер данных
            OleDbDataAdapter da = new OleDbDataAdapter();
            da.SelectCommand = cmd;
            // Заполняем объект данными
            DataTable tbl = new DataTable();
            da.Fill(tbl);
            tbl.Columns.Add("FULLNAME", typeof(String));
            for (int i = 0; i < tbl.Rows.Count; i++)
            {
                #region меняем кодировку с досовской
                //tbl.Rows[i]["NAME"] = Encoding.GetEncoding(866).GetString(
                //    Encoding.Default.GetBytes(tbl.Rows[i]["NAME"].ToString()));
                //tbl.Rows[i]["SOCR"] = Encoding.GetEncoding(866).GetString(
                //    Encoding.Default.GetBytes(tbl.Rows[i]["SOCR"].ToString()));
                #endregion

                //склеить название и сокращение
                tbl.Rows[i]["CODE"] = tbl.Rows[i]["CODE"].ToString().Substring(0, 11);
                string socr = "";
                if (tbl.Rows[i]["SOCR"] != null)
                {
                    socr = tbl.Rows[i]["SOCR"].ToString().Trim();
                    tbl.Rows[i]["SOCR"] = socr;
                }
                string name = "";
                if (tbl.Rows[i]["NAME"] != null)
                {
                    name = tbl.Rows[i]["NAME"].ToString().Trim();
                    tbl.Rows[i]["NAME"] = name;
                }
                tbl.Rows[i]["FULLNAME"] = name + " " + socr;
            }
            return tbl;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cbxIgnoreCityDistrict_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void cbClearAddressSpace_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if (cb.Checked)
            {
                DialogResult dialogResult = MessageBox.Show("Вы уверены?", "Очистить адресное пространство", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.No)
                {
                    cb.Checked = false;
                }
            }
        }
    }
}

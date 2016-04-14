using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IBM.Data.Informix;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.Globalization;
using System.Threading;
using Bars.CloudRoster.Contracts.Data;
using Bars.CloudRoster.Contracts.Service;
using Bars.CloudRoster.ClientProxy.Service;
using Bars.CloudRoster.ClientProxy.RosterServiceReference;
//using STCLINE.KP50.HostMan.RosterService;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Security.Cryptography.X509Certificates;

namespace STCLINE.KP50.HostMan.AddressService
{
    public partial class FrmAddressService : Form
    {
        private IfxConnection conn;
        private DataTable regionTable;
        private DataTable districtTable;
        private DataTable settlementTable;
        private DataTable streetTable;
        private DataTable houseTable;
        private string level;


        public FrmAddressService(string tconnectionString)
        {
            InitializeComponent();
            regionTable = new DataTable();
            districtTable = new DataTable();
            settlementTable = new DataTable();
            streetTable = new DataTable();
            houseTable = new DataTable();

            level = "";
            conn = new IfxConnection() { ConnectionString = tconnectionString };
            try
            {
                conn.Open();
                string sqlString = "SELECT * FROM s_stat ";
                IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
                regionTable = ConvertToTitleCase("stat", dt.resultData);
                cbRegion.DataSource = regionTable.DefaultView;
                cbRegion.DisplayMember = "stat";
                cbRegion.ValueMember = "nzp_stat";
                cbRegion.BindingContext = this.BindingContext;
                cbRegion.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }

        private void cbRegion_SelectionChangeCommitted(object sender, EventArgs e)
        {
            level = "region";

            houseTable.Rows.Clear();
            streetTable.Rows.Clear();
            settlementTable.Rows.Clear();
            districtTable.Rows.Clear();

            DataRowView dr = (DataRowView)cbRegion.SelectedItem;
            string nzp_stat = dr.Row["nzp_stat"].ToString();
            string sqlString = "select unique t.* from s_rajon r inner join s_town t on r.nzp_town=t.nzp_town where t.nzp_stat = " + nzp_stat;
            IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
            districtTable = ConvertToTitleCase("town", dt.resultData);
            if (dt.resultData.Rows.Count != 0)
            {
                cbDistrict.DataSource = districtTable.DefaultView;
                cbDistrict.DisplayMember = "town";
                cbDistrict.ValueMember = "nzp_town";
                cbDistrict.BindingContext = this.BindingContext;
                cbDistrict.SelectedIndex = -1;
                cbDistrict.Enabled = true;
                btnClearDistrict.Enabled = true;
            }
            cbHouse.SelectedIndex = -1;
            cbHouse.Enabled = false;
            btnClearHouse.Enabled = false;
            cbStreet.SelectedIndex = -1;
            cbStreet.Enabled = false;
            btnClearStreet.Enabled = false;
            cbSettlement.SelectedIndex = -1;
            cbSettlement.Enabled = false;
            btnClearSettlement.Enabled = false;
        }

        private void cbDistrict_SelectionChangeCommitted(object sender, EventArgs e)
        {
            level = "district";

            houseTable.Rows.Clear();
            streetTable.Rows.Clear();
            settlementTable.Rows.Clear();

            DataRowView dr = (DataRowView)cbDistrict.SelectedItem;
            string nzp_town = dr.Row["nzp_town"].ToString();
            string sqlString = "SELECT * FROM s_rajon WHERE nzp_town = " + nzp_town;
            IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
            settlementTable = ConvertToTitleCase("rajon", dt.resultData);
            if (dt.resultData.Rows.Count != 0)
            {
                cbSettlement.DataSource = settlementTable.DefaultView;
                cbSettlement.DisplayMember = "rajon";
                cbSettlement.ValueMember = "nzp_raj";
                cbSettlement.BindingContext = this.BindingContext;
                cbSettlement.SelectedIndex = -1;
                cbSettlement.Enabled = true;
                btnClearSettlement.Enabled = true;
            }

            sqlString = "SELECT * FROM s_ulica WHERE nzp_raj = " + nzp_town;
            dt = ClassDBUtils.OpenSQL(sqlString, conn);
            if (dt.resultData.Rows.Count != 0)
            {
                streetTable = ConvertToTitleCase("ulica", dt.resultData);
                if (dt.resultData.Rows.Count != 0)
                {
                    cbStreet.DataSource = streetTable.DefaultView;
                    cbStreet.DisplayMember = "ulica";
                    cbStreet.ValueMember = "nzp_ul";
                    cbStreet.BindingContext = this.BindingContext;
                    cbStreet.SelectedIndex = -1;
                    cbStreet.Enabled = true;
                    btnClearStreet.Enabled = true;
                }
            }
            else
            {
                cbStreet.SelectedIndex = -1;
                cbStreet.Enabled = false;
                btnClearStreet.Enabled = false;
            }

            cbHouse.SelectedIndex = -1;
            cbHouse.Enabled = false;
            btnClearHouse.Enabled = false;
        }

        private void cbSettlement_SelectionChangeCommitted(object sender, EventArgs e)
        {
            level = "settlement";

            houseTable.Rows.Clear();
            streetTable.Rows.Clear();

            DataRowView dr = (DataRowView)cbSettlement.SelectedItem;
            string nzp_raj = dr.Row["nzp_raj"].ToString();
            string sqlString = "SELECT * FROM s_ulica WHERE nzp_raj = " + nzp_raj;
            IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
            streetTable = ConvertToTitleCase("ulica", dt.resultData);
            if (dt.resultData.Rows.Count != 0)
            {
                cbStreet.DataSource = streetTable.DefaultView;
                cbStreet.DisplayMember = "ulica";
                cbStreet.ValueMember = "nzp_ul";
                cbStreet.BindingContext = this.BindingContext;
                cbStreet.SelectedIndex = -1;
                cbStreet.Enabled = true;
                btnClearStreet.Enabled = true;
            }
        }

        private void cbStreet_SelectionChangeCommitted(object sender, EventArgs e)
        {
            level = "street";

            houseTable.Rows.Clear();

            DataRowView dr = (DataRowView)cbStreet.SelectedItem;
            string nzp_ul = dr.Row["nzp_ul"].ToString();
            string sqlString = "SELECT * FROM dom WHERE nzp_ul = " + nzp_ul;
            IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
            dt.resultData.Columns.Add("full_name");
            for (int i = 0; i < dt.resultData.Rows.Count; i++)
            {
                if (dt.resultData.Rows[i]["nkor"].ToString().Trim() != "-")
                    dt.resultData.Rows[i]["full_name"] = dt.resultData.Rows[i]["ndom"].ToString().Trim() + " Корп. " + dt.resultData.Rows[i]["nkor"].ToString().Trim();
                else
                    dt.resultData.Rows[i]["full_name"] = dt.resultData.Rows[i]["ndom"].ToString().Trim();
            }
            houseTable = dt.resultData;
            if (dt.resultData.Rows.Count != 0)
            {
                cbHouse.DataSource = houseTable.DefaultView;
                cbHouse.DisplayMember = "full_name";
                cbHouse.ValueMember = "nzp_dom";
                cbHouse.BindingContext = this.BindingContext;
                cbHouse.SelectedIndex = -1;
                cbHouse.Enabled = true;
                btnClearHouse.Enabled = true;
            }
            else
            {
                cbHouse.SelectedIndex = -1;
                cbHouse.Enabled = false;
                btnClearHouse.Enabled = false;
            }
            btnOk.Enabled = true;
        }

        private void cbHouse_SelectionChangeCommitted(object sender, EventArgs e)
        {
            level = "house";
            btnOk.Enabled = true;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            conn.Close();
            Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (level == "")
            {
                MessageBox.Show("Выберите объект");
                return;
            }

            try
            {
                var client = new RosterServiceProxy();

                //string login = "Billing";
                //string pass = "testpass";


                if (level == "house")
                {
                    #region загрузка данных на сервис
                    InputAddressData[] data = new InputAddressData[1];
                    DataRowView houseDrw = (DataRowView)cbHouse.SelectedItem;
                    DataRow houseDr = houseDrw.Row;
                    DataRowView streetDrw = (DataRowView)cbStreet.SelectedItem;
                    DataRow streetDr = streetDrw.Row;
                    DataRowView settlementDrw = (DataRowView)cbSettlement.SelectedItem;
                    DataRow settlementDr = settlementDrw.Row;
                    DataRowView districtDrw = (DataRowView)cbDistrict.SelectedItem;
                    DataRow districtDr = districtDrw.Row;
                    DataRowView regionDrw = (DataRowView)cbRegion.SelectedItem;
                    DataRow regionDr = regionDrw.Row;
                    string cityName = "";
                    if (settlementDr["rajon"].ToString() == "-")
                        cityName = districtDr["name"].ToString();
                    else
                        cityName = settlementDr["name"].ToString();
                    string nkor = "";
                    if (houseDr["nkor"].ToString().Trim() != "-")
                        nkor = houseDr["nkor"].ToString().Trim();
                    data[0] = new InputAddressData()
                    {
                        ExtId = houseDr["nzp_dom"].ToString(),
                        RegionName = regionDr["name"].ToString(),
                        CityName = cityName,
                        StreetName = streetDr["name"].ToString(),
                        HouseNumber = houseDr["ndom"].ToString().Trim(),
                        KorpNumber = nkor,
                        KladrStreetCode = streetDr["soato"].ToString(),
                        PostCode = houseDr["indecs"].ToString().Trim()
                    };
                    RosterRequestReport response1 = client.SendAddressData(data);
                    int requestId = response1.RequestId;
                    #endregion

                    #region получение результатов запроса
                    RosterCompareResult[] response2 = client.GetComparedResult(requestId);
                    #endregion

                    #region выгрузка данных с сервиса
                    if (response2[0].ComparisonStatus == ComparisonStatus.Mapped)
                    {
                        ClientLoadParam baseparams = new ClientLoadParam()
                        {
                            Limit = 1,
                            Start = 0
                        };
                        baseparams.AddFilter(AddressHandbookFields.HouseNumber, houseDr["ndom"].ToString().Trim());
                        //baseparams.AddFilter(AddressHandbookFields.KorpNumber, nkor);
                        baseparams.AddFilter(AddressHandbookFields.StreetName, streetDr["name"].ToString());
                        baseparams.AddFilter(AddressHandbookFields.CityName, cityName);
                        baseparams.AddFilter(AddressHandbookFields.RegionName, regionDr["name"].ToString());

                        var response3 = client.GetAddressData(baseparams);

                        string sqlString = "SELECT * FROM ext_sys_addr_space where kod_sys = 1 and nzp_dom = " + houseDr["nzp_dom"].ToString();
                        IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
                        bool update;
                        IfxTransaction transactionID = conn.BeginTransaction();
                        try
                        {
                            if (dt.resultData.Rows.Count != 0)
                            {
                                update = true;
                                sqlString = "UPDATE ext_sys_addr_space SET ( full_address, region_short_name, region_name, region_aoguid, zone_short_name, " +
                                        " zone_name, zone_aoguid, city_short_name, city_name, city_aoguid, street_short_name, street_name, street_aoguid, " +
                                        " house_number, house_guid, struct_number, korp_number, okato, post_code, latitude, longitude ) = ( " +
                                        " ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? ) where kod_sys = 1 and nzp_dom = ? ";
                            }
                            else
                            {
                                update = false;
                                sqlString = "INSERT INTO ext_sys_addr_space ( full_address, region_short_name, region_name, region_aoguid, zone_short_name, " +
                                        " zone_name, zone_aoguid, city_short_name, city_name, city_aoguid, street_short_name, street_name, street_aoguid, " +
                                        " house_number, house_guid, struct_number, korp_number, okato, post_code, latitude, longitude, nzp_dom, kod_sys ) VALUES ( " +
                                        " ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? ) ";
                            }
                            IfxCommand ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                            IfxCommandQueryParam ifxPrm = new IfxCommandQueryParam(ifxCommand);

                            ifxPrm.AddParam("full_address", response3[0].FullAddress);
                            ifxPrm.AddParam("region_short_name", response3[0].RegionShortName);
                            ifxPrm.AddParam("region_name", response3[0].RegionName);
                            ifxPrm.AddParam("region_aoguid", response3[0].RegionAoguid);
                            ifxPrm.AddParam("zone_short_name", response3[0].ZoneShortName);
                            ifxPrm.AddParam("zone_name", response3[0].ZoneName);
                            ifxPrm.AddParam("zone_aoguid", response3[0].ZoneAoguid);
                            ifxPrm.AddParam("city_short_name", response3[0].CityShortName);
                            ifxPrm.AddParam("city_name", response3[0].CityName);
                            ifxPrm.AddParam("city_aoguid", response3[0].CityAoguid);
                            ifxPrm.AddParam("street_short_name", response3[0].StreetShortName);
                            ifxPrm.AddParam("street_name", response3[0].StreetName);
                            ifxPrm.AddParam("street_aoguid", response3[0].StreetAoguid);
                            ifxPrm.AddParam("house_number", response3[0].HouseNumber);
                            ifxPrm.AddParam("house_guid", response3[0].HouseGuid);
                            ifxPrm.AddParam("struct_number", response3[0].StructNumber);
                            ifxPrm.AddParam("korp_number", response3[0].KorpNumber);
                            ifxPrm.AddParam("okato", response3[0].Okato);
                            ifxPrm.AddParam("post_code", response3[0].PostCode);
                            ifxPrm.AddParam("latitude", response3[0].Latitude);
                            ifxPrm.AddParam("longitude", response3[0].Longitude);
                            ifxPrm.AddParam("nzp_dom", houseDr["nzp_dom"].ToString());
                            if (!update)
                            {
                                ifxPrm.AddParam("kod_sys", "1");
                            }

                            ifxCommand.ExecuteNonQuery();
                            transactionID.Commit();
                        }
                        catch (Exception ex)
                        {
                            transactionID.Rollback();
                            MessageBox.Show(ex.Message);
                        }
                    #endregion
                    }
                }
                if (level == "street")
                {
                    #region загрузка данных на сервис
                    InputAddressData[] data = new InputAddressData[houseTable.Rows.Count];
                    DataRowView streetDrw = (DataRowView)cbStreet.SelectedItem;
                    DataRow streetDr = streetDrw.Row;
                    DataRowView settlementDrw = (DataRowView)cbSettlement.SelectedItem;
                    DataRow settlementDr = settlementDrw.Row;
                    DataRowView districtDrw = (DataRowView)cbDistrict.SelectedItem;
                    DataRow districtDr = districtDrw.Row;
                    DataRowView regionDrw = (DataRowView)cbRegion.SelectedItem;
                    DataRow regionDr = regionDrw.Row;
                    string cityName = "";
                    if (settlementDr["rajon"].ToString() == "-")
                        cityName = districtDr["name"].ToString();
                    else
                        cityName = settlementDr["name"].ToString();

                    for (int i = 0; i < houseTable.Rows.Count; i++)
                    {
                        DataRow houseDr = houseTable.Rows[i];
                        string nkor = "";
                        if (houseDr["nkor"].ToString().Trim() != "-")
                            nkor = houseDr["nkor"].ToString();
                        data[i] = new InputAddressData()
                        {
                            ExtId = houseDr["nzp_dom"].ToString(),
                            RegionName = regionDr["name"].ToString(),
                            CityName = cityName,
                            StreetName = streetDr["name"].ToString(),
                            HouseNumber = houseDr["ndom"].ToString().Trim(),
                            KorpNumber = nkor,
                            KladrStreetCode = streetDr["soato"].ToString(),
                            PostCode = houseDr["indecs"].ToString().Trim()
                        };
                    }

                    RosterRequestReport response1 = client.SendAddressData(data);
                    int requestId = response1.RequestId;
                    #endregion

                    #region получение результатов запроса
                    RosterCompareResult[] response2 = client.GetComparedResult(requestId);
                    #endregion

                    #region выгрузка данных с сервиса
                    ClientLoadParam baseparams = new ClientLoadParam()
                    {
                        Start = 0,
                        Limit = 1000,
                    };
                    baseparams.AddFilter("StreetName", streetDr["name"].ToString());
                    baseparams.AddFilter("CityName", cityName);
                    baseparams.AddFilter("RegionName", regionDr["name"].ToString());

                    OutputAddressData[] response3 = client.GetAddressData(baseparams);

                    foreach (OutputAddressData resp in response3)
                    {
                        string nzp_dom = "";
                        ComparisonStatus stat = ComparisonStatus.NotMapped;
                        foreach (RosterCompareResult reqRes in response2)
                        {
                            if (reqRes.Uid == resp.Id.ToString())
                            {
                                nzp_dom = reqRes.ExtId;
                                stat = reqRes.ComparisonStatus;
                                break;
                            }
                        }

                        if (stat == ComparisonStatus.Mapped)
                        {

                            IfxTransaction transactionID = conn.BeginTransaction();
                            try
                            {
                                string sqlString = "SELECT * FROM ext_sys_addr_space where kod_sys = 1 and nzp_dom = " + nzp_dom;
                                IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
                                bool update;
                                if (dt.resultData.Rows.Count != 0)
                                {
                                    update = true;
                                    sqlString = "UPDATE ext_sys_addr_space SET ( full_address, region_short_name, region_name, region_aoguid, zone_short_name, " +
                                            " zone_name, zone_aoguid, city_short_name, city_name, city_aoguid, street_short_name, street_name, street_aoguid, " +
                                            " house_number, gouse_guid, struct_number, korp_number, okato, post_code, latitude, longitude ) = ( " +
                                            " ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? ) where kod_sys = 1 and nzp_dom = ? ";
                                }
                                else
                                {
                                    update = false;
                                    sqlString = "INSERT INTO ext_sys_addr_space ( full_address, region_short_name, region_name, region_aoguid, zone_short_name, " +
                                            " zone_name, zone_aoguid, city_short_name, city_name, city_aoguid, street_short_name, street_name, street_aoguid, " +
                                            " house_number, gouse_guid, struct_number, korp_number, okato, post_code, latitude, longitude, nzp_dom, kod_sys ) VALUES ( " +
                                            " ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? ) ";
                                }
                                IfxCommand ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                                IfxCommandQueryParam ifxPrm = new IfxCommandQueryParam(ifxCommand);

                                ifxPrm.AddParam("full_address", resp.FullAddress);
                                ifxPrm.AddParam("region_short_name", resp.RegionShortName);
                                ifxPrm.AddParam("region_name", resp.RegionName);
                                ifxPrm.AddParam("region_aoguid", resp.RegionAoguid);
                                ifxPrm.AddParam("zone_short_name", resp.ZoneShortName);
                                ifxPrm.AddParam("zone_name", resp.ZoneName);
                                ifxPrm.AddParam("zone_aoguid", resp.ZoneAoguid);
                                ifxPrm.AddParam("city_short_name", resp.CityShortName);
                                ifxPrm.AddParam("city_name", resp.CityName);
                                ifxPrm.AddParam("city_aoguid", resp.CityAoguid);
                                ifxPrm.AddParam("street_short_name", resp.StreetShortName);
                                ifxPrm.AddParam("street_name", resp.StreetName);
                                ifxPrm.AddParam("street_aoguid", resp.StreetAoguid);
                                ifxPrm.AddParam("house_number", resp.HouseNumber);
                                ifxPrm.AddParam("gouse_guid", resp.HouseGuid);
                                ifxPrm.AddParam("struct_number", resp.StructNumber);
                                ifxPrm.AddParam("korp_number", resp.KorpNumber);
                                ifxPrm.AddParam("okato", resp.Okato);
                                ifxPrm.AddParam("post_code", resp.PostCode);
                                ifxPrm.AddParam("latitude", resp.Latitude);
                                ifxPrm.AddParam("longitude", resp.Longitude);
                                ifxPrm.AddParam("nzp_dom", nzp_dom);
                                if (!update)
                                {
                                    ifxPrm.AddParam("kod_sys", "1");
                                }

                                ifxCommand.ExecuteNonQuery();
                                transactionID.Commit();
                            }
                            catch (Exception ex)
                            {
                                transactionID.Rollback();
                                MessageBox.Show(ex.Message);
                            }
                        }
                    }
                    #endregion
                }
                if (level == "settlement")
                {
                    #region загрузка данных на сервис
                    List<InputAddressData> dataList = new List<InputAddressData>();

                    DataRowView settlementDrw = (DataRowView)cbSettlement.SelectedItem;
                    DataRow settlementDr = settlementDrw.Row;
                    DataRowView districtDrw = (DataRowView)cbDistrict.SelectedItem;
                    DataRow districtDr = districtDrw.Row;
                    DataRowView regionDrw = (DataRowView)cbRegion.SelectedItem;
                    DataRow regionDr = regionDrw.Row;
                    string cityName = "";
                    if (settlementDr["rajon"].ToString() == "-")
                        cityName = districtDr["name"].ToString();
                    else
                        cityName = settlementDr["name"].ToString();

                    for (int s = 0; s < streetTable.Rows.Count; s++)
                    {
                        string nzp_ul = streetTable.Rows[s]["nzp_ul"].ToString();
                        string sqlString = "SELECT * FROM dom WHERE nzp_ul = " + nzp_ul;
                        DataRow streetDr = streetTable.Rows[s];
                        IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
                        houseTable.Rows.Clear();
                        houseTable = dt.resultData;

                        for (int i = 0; i < houseTable.Rows.Count; i++)
                        {
                            DataRow houseDr = houseTable.Rows[i];
                            string nkor = "";
                            if (houseDr["nkor"].ToString().Trim() != "-")
                                nkor = houseDr["nkor"].ToString();
                            InputAddressData temp = new InputAddressData()
                            {
                                ExtId = houseDr["nzp_dom"].ToString(),
                                RegionName = regionDr["name"].ToString(),
                                CityName = cityName,
                                StreetName = streetDr["name"].ToString(),
                                HouseNumber = houseDr["ndom"].ToString().Trim(),
                                KorpNumber = nkor,
                                KladrStreetCode = streetDr["soato"].ToString(),
                                PostCode = houseDr["indecs"].ToString().Trim()
                            };
                            dataList.Add(temp);
                        }
                    }

                    #endregion
                    List<InputAddressData> tList = new List<InputAddressData>();
                    int counter = 0;
                    int start = 0;
                    for (int i = 0; i < dataList.Count; i++)
                    {
                        counter++;
                        if (counter < 1000)
                        {
                            tList.Add(dataList[i]);
                        }
                        else
                        {
                            counter = 0;
                            InputAddressData[] data = tList.ToArray();
                            tList.Clear();

                            RosterRequestReport response1 = client.SendAddressData(data);
                            int requestId = response1.RequestId;

                            #region получение результатов запроса
                            RosterCompareResult[] response2 = client.GetComparedResult(requestId);
                            #endregion

                            #region выгрузка данных с сервиса
                            ClientLoadParam baseparams = new ClientLoadParam()
                            {
                                Start = start,
                                Limit = start + 1000,
                            };
                            baseparams.AddFilter("CityName", cityName);
                            baseparams.AddFilter("RegionName", regionDr["name"].ToString());

                            OutputAddressData[] response3 = client.GetAddressData(baseparams);

                            foreach (OutputAddressData resp in response3)
                            {
                                string nzp_dom = "";
                                ComparisonStatus stat = ComparisonStatus.NotMapped;
                                foreach (RosterCompareResult reqRes in response2)
                                {
                                    if (reqRes.Uid == resp.Id.ToString())
                                    {
                                        nzp_dom = reqRes.ExtId;
                                        stat = reqRes.ComparisonStatus;
                                        break;
                                    }
                                }

                                if (stat == ComparisonStatus.Mapped)
                                {

                                    IfxTransaction transactionID = conn.BeginTransaction();
                                    try
                                    {
                                        string sqlString = "SELECT * FROM ext_sys_addr_space where kod_sys = 1 and nzp_dom = " + nzp_dom;
                                        IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
                                        bool update;
                                        if (dt.resultData.Rows.Count != 0)
                                        {
                                            update = true;
                                            sqlString = "UPDATE ext_sys_addr_space SET ( full_address, region_short_name, region_name, region_aoguid, zone_short_name, " +
                                                    " zone_name, zone_aoguid, city_short_name, city_name, city_aoguid, street_short_name, street_name, street_aoguid, " +
                                                    " house_number, gouse_guid, struct_number, korp_number, okato, post_code, latitude, longitude ) = ( " +
                                                    " ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? ) where kod_sys = 1 and nzp_dom = ? ";
                                        }
                                        else
                                        {
                                            update = false;
                                            sqlString = "INSERT INTO ext_sys_addr_space ( full_address, region_short_name, region_name, region_aoguid, zone_short_name, " +
                                                    " zone_name, zone_aoguid, city_short_name, city_name, city_aoguid, street_short_name, street_name, street_aoguid, " +
                                                    " house_number, gouse_guid, struct_number, korp_number, okato, post_code, latitude, longitude, nzp_dom, kod_sys ) VALUES ( " +
                                                    " ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? ) ";
                                        }
                                        IfxCommand ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                                        IfxCommandQueryParam ifxPrm = new IfxCommandQueryParam(ifxCommand);

                                        ifxPrm.AddParam("full_address", resp.FullAddress);
                                        ifxPrm.AddParam("region_short_name", resp.RegionShortName);
                                        ifxPrm.AddParam("region_name", resp.RegionName);
                                        ifxPrm.AddParam("region_aoguid", resp.RegionAoguid);
                                        ifxPrm.AddParam("zone_short_name", resp.ZoneShortName);
                                        ifxPrm.AddParam("zone_name", resp.ZoneName);
                                        ifxPrm.AddParam("zone_aoguid", resp.ZoneAoguid);
                                        ifxPrm.AddParam("city_short_name", resp.CityShortName);
                                        ifxPrm.AddParam("city_name", resp.CityName);
                                        ifxPrm.AddParam("city_aoguid", resp.CityAoguid);
                                        ifxPrm.AddParam("street_short_name", resp.StreetShortName);
                                        ifxPrm.AddParam("street_name", resp.StreetName);
                                        ifxPrm.AddParam("street_aoguid", resp.StreetAoguid);
                                        ifxPrm.AddParam("house_number", resp.HouseNumber);
                                        ifxPrm.AddParam("gouse_guid", resp.HouseGuid);
                                        ifxPrm.AddParam("struct_number", resp.StructNumber);
                                        ifxPrm.AddParam("korp_number", resp.KorpNumber);
                                        ifxPrm.AddParam("okato", resp.Okato);
                                        ifxPrm.AddParam("post_code", resp.PostCode);
                                        ifxPrm.AddParam("latitude", resp.Latitude);
                                        ifxPrm.AddParam("longitude", resp.Longitude);
                                        ifxPrm.AddParam("nzp_dom", nzp_dom);
                                        if (!update)
                                        {
                                            ifxPrm.AddParam("kod_sys", "1");
                                        }

                                        ifxCommand.ExecuteNonQuery();
                                        transactionID.Commit();
                                    }
                                    catch (Exception ex)
                                    {
                                        transactionID.Rollback();
                                        MessageBox.Show(ex.Message);
                                    }
                                }
                            }
                            start += 1000;
                        }
                    }
                            #endregion
                }
                if (level == "district")
                {
                    #region загрузка данных на сервис
                    List<InputAddressData> dataList = new List<InputAddressData>();

                    DataRowView districtDrw = (DataRowView)cbDistrict.SelectedItem;
                    DataRow districtDr = districtDrw.Row;
                    DataRowView regionDrw = (DataRowView)cbRegion.SelectedItem;
                    DataRow regionDr = regionDrw.Row;

                    for (int l = 0; l < settlementTable.Rows.Count; l++)
                    {
                        DataRow settlementDr = settlementTable.Rows[l];
                        string nzp_raj = settlementTable.Rows[l]["nzp_raj"].ToString();
                        string sqlString = "SELECT * FROM s_ulica WHERE nzp_raj = " + nzp_raj;
                        streetTable.Rows.Clear();
                        IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
                        streetTable = ConvertToTitleCase("ulica", dt.resultData);

                        string cityName = "";
                        if (settlementDr["rajon"].ToString() == "-")
                            cityName = districtDr["name"].ToString();
                        else
                            cityName = settlementDr["name"].ToString();

                        for (int s = 0; s < streetTable.Rows.Count; s++)
                        {
                            string nzp_ul = streetTable.Rows[s]["nzp_ul"].ToString();
                            sqlString = "SELECT * FROM dom WHERE nzp_ul = " + nzp_ul;
                            DataRow streetDr = streetTable.Rows[s];
                            dt = ClassDBUtils.OpenSQL(sqlString, conn);
                            houseTable.Rows.Clear();
                            houseTable = dt.resultData;

                            for (int i = 0; i < houseTable.Rows.Count; i++)
                            {
                                DataRow houseDr = houseTable.Rows[i];
                                string nkor = "";
                                if (houseDr["nkor"].ToString().Trim() != "-")
                                    nkor = houseDr["nkor"].ToString();
                                InputAddressData temp = new InputAddressData()
                                {
                                    ExtId = houseDr["nzp_dom"].ToString(),
                                    RegionName = regionDr["name"].ToString(),
                                    CityName = cityName,
                                    StreetName = streetDr["name"].ToString(),
                                    HouseNumber = houseDr["ndom"].ToString().Trim(),
                                    KorpNumber = nkor,
                                    KladrStreetCode = streetDr["soato"].ToString(),
                                    PostCode = houseDr["indecs"].ToString().Trim()
                                };
                                dataList.Add(temp);
                            }
                        }
                    }

                    #endregion

                    IfxTransaction transactionID = conn.BeginTransaction();
                    List<InputAddressData> tList = new List<InputAddressData>();
                    int counter = 0;
                    int start = 0;
                    for (int i = 0; i < dataList.Count; i++)
                    {
                        counter++;
                        if (counter < 50)
                        {
                            tList.Add(dataList[i]);
                        }
                        else
                        {
                            counter = 0;
                            
                            InputAddressData[] data = tList.ToArray();
                            tList.Clear();

                            RosterRequestReport response1 = client.SendAddressData(data);
                            int requestId = response1.RequestId;

                            #region получение результатов запроса
                            RosterCompareResult[] response2 = client.GetComparedResult(requestId);
                            #endregion

                            #region выгрузка данных с сервиса
                            ClientLoadParam baseparams = new ClientLoadParam()
                            {
                                Start = start,
                                Limit = start + 50,
                            };
                            baseparams.AddFilter("CityName", districtDr["name"].ToString());
                            baseparams.AddFilter("RegionName", regionDr["name"].ToString());

                            OutputAddressData[] response3 = client.GetAddressData(baseparams);

                            foreach (OutputAddressData resp in response3)
                            {
                                string nzp_dom = "";
                                ComparisonStatus stat = ComparisonStatus.NotMapped;
                                foreach (RosterCompareResult reqRes in response2)
                                {
                                    if (reqRes.Uid == resp.Id.ToString())
                                    {
                                        nzp_dom = reqRes.ExtId;
                                        stat = reqRes.ComparisonStatus;
                                        break;
                                    }
                                }

                                if (stat == ComparisonStatus.Mapped)
                                {

                                    try
                                    {
                                        string sqlString = "SELECT * FROM ext_sys_addr_space where kod_sys = 1 and nzp_dom = " + nzp_dom;
                                        IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn, transactionID);
                                        bool update;
                                        if (dt.resultData.Rows.Count != 0)
                                        {
                                            update = true;
                                            sqlString = "UPDATE ext_sys_addr_space SET ( full_address, region_short_name, region_name, region_aoguid, zone_short_name, " +
                                                    " zone_name, zone_aoguid, city_short_name, city_name, city_aoguid, street_short_name, street_name, street_aoguid, " +
                                                    " house_number, house_guid, struct_number, korp_number, okato, post_code, latitude, longitude ) = ( " +
                                                    " ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? ) where kod_sys = 1 and nzp_dom = ? ";
                                        }
                                        else
                                        {
                                            update = false;
                                            sqlString = "INSERT INTO ext_sys_addr_space ( full_address, region_short_name, region_name, region_aoguid, zone_short_name, " +
                                                    " zone_name, zone_aoguid, city_short_name, city_name, city_aoguid, street_short_name, street_name, street_aoguid, " +
                                                    " house_number, house_guid, struct_number, korp_number, okato, post_code, latitude, longitude, nzp_dom, kod_sys ) VALUES ( " +
                                                    " ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? ) ";
                                        }
                                        IfxCommand ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                                        IfxCommandQueryParam ifxPrm = new IfxCommandQueryParam(ifxCommand);

                                        ifxPrm.AddParam("full_address", resp.FullAddress);
                                        ifxPrm.AddParam("region_short_name", resp.RegionShortName);
                                        ifxPrm.AddParam("region_name", resp.RegionName);
                                        ifxPrm.AddParam("region_aoguid", resp.RegionAoguid);
                                        ifxPrm.AddParam("zone_short_name", resp.ZoneShortName);
                                        ifxPrm.AddParam("zone_name", resp.ZoneName);
                                        ifxPrm.AddParam("zone_aoguid", resp.ZoneAoguid);
                                        ifxPrm.AddParam("city_short_name", resp.CityShortName);
                                        ifxPrm.AddParam("city_name", resp.CityName);
                                        ifxPrm.AddParam("city_aoguid", resp.CityAoguid);
                                        ifxPrm.AddParam("street_short_name", resp.StreetShortName);
                                        ifxPrm.AddParam("street_name", resp.StreetName);
                                        ifxPrm.AddParam("street_aoguid", resp.StreetAoguid);
                                        ifxPrm.AddParam("house_number", resp.HouseNumber);
                                        ifxPrm.AddParam("house_guid", resp.HouseGuid);
                                        ifxPrm.AddParam("struct_number", resp.StructNumber);
                                        ifxPrm.AddParam("korp_number", resp.KorpNumber);
                                        ifxPrm.AddParam("okato", resp.Okato);
                                        ifxPrm.AddParam("post_code", resp.PostCode);
                                        ifxPrm.AddParam("latitude", resp.Latitude);
                                        ifxPrm.AddParam("longitude", resp.Longitude);
                                        ifxPrm.AddParam("nzp_dom", nzp_dom);
                                        if (!update)
                                        {
                                            ifxPrm.AddParam("kod_sys", "1");
                                        }

                                        ifxCommand.ExecuteNonQuery();
                                    }
                                    catch (Exception ex)
                                    {
                                        transactionID.Rollback();
                                        MessageBox.Show(ex.Message);
                                    }
                                }
                            }
                            start += 50;
                        }
                    }
                    transactionID.Commit();
                            #endregion
                }
                if (level == "region")
                {
                    #region загрузка данных на сервис
                    List<InputAddressData> dataList = new List<InputAddressData>();

                    DataRowView regionDrw = (DataRowView)cbRegion.SelectedItem;
                    DataRow regionDr = regionDrw.Row;

                    for (int r = 0; r < districtTable.Rows.Count; r++)
                    {
                        DataRow districtDr = districtTable.Rows[r];
                        string nzp_town = districtTable.Rows[r]["nzp_town"].ToString();
                        string sqlString = "SELECT * FROM s_rajon WHERE nzp_town = " + nzp_town;
                        settlementTable.Rows.Clear();
                        IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
                        settlementTable = ConvertToTitleCase("rajon", dt.resultData);

                        for (int l = 0; l < settlementTable.Rows.Count; l++)
                        {
                            DataRow settlementDr = settlementTable.Rows[l];
                            string nzp_raj = settlementTable.Rows[l]["nzp_raj"].ToString();
                            sqlString = "SELECT * FROM s_ulica WHERE nzp_raj = " + nzp_raj;
                            streetTable.Rows.Clear();
                            dt = ClassDBUtils.OpenSQL(sqlString, conn);
                            streetTable = ConvertToTitleCase("ulica", dt.resultData);

                            string cityName = "";
                            if (settlementDr["rajon"].ToString() == "-")
                                cityName = districtDr["name"].ToString();
                            else
                                cityName = settlementDr["name"].ToString();

                            for (int s = 0; s < streetTable.Rows.Count; s++)
                            {
                                string nzp_ul = streetTable.Rows[s]["nzp_ul"].ToString();
                                sqlString = "SELECT * FROM dom WHERE nzp_ul = " + nzp_ul;
                                DataRow streetDr = streetTable.Rows[s];
                                dt = ClassDBUtils.OpenSQL(sqlString, conn);
                                houseTable.Rows.Clear();
                                houseTable = dt.resultData;

                                for (int i = 0; i < houseTable.Rows.Count; i++)
                                {
                                    DataRow houseDr = houseTable.Rows[i];
                                    string nkor = "";
                                    if (houseDr["nkor"].ToString().Trim() != "-")
                                        nkor = houseDr["nkor"].ToString();
                                    InputAddressData temp = new InputAddressData()
                                    {
                                        ExtId = houseDr["nzp_dom"].ToString(),
                                        RegionName = regionDr["name"].ToString(),
                                        CityName = cityName,
                                        StreetName = streetDr["name"].ToString(),
                                        HouseNumber = houseDr["ndom"].ToString().Trim(),
                                        KorpNumber = nkor,
                                        KladrStreetCode = streetDr["soato"].ToString(),
                                        PostCode = houseDr["indecs"].ToString().Trim()
                                    };
                                    dataList.Add(temp);
                                }
                            }
                        }
                    }

                    #endregion

                    List<InputAddressData> tList = new List<InputAddressData>();
                    int counter = 0;
                    int start = 0;
                    IfxTransaction transactionID = conn.BeginTransaction();
                    for (int i = 0; i < dataList.Count; i++)
                    {
                        counter++;
                        if (counter < 1000)
                        {
                            tList.Add(dataList[i]);
                        }
                        else
                        {
                            counter = 0;
                            InputAddressData[] data = tList.ToArray();
                            tList.Clear();
                            RosterRequestReport response1 = client.SendAddressData(data);
                            int requestId = response1.RequestId;

                            #region получение результатов запроса
                            RosterCompareResult[] response2 = client.GetComparedResult(requestId);
                            #endregion

                            #region выгрузка данных с сервиса

                            ClientLoadParam baseparams = new ClientLoadParam()
                            {
                                Start = start,
                                Limit = start + 1000,
                            };
                            baseparams.AddFilter("RegionName", regionDr["name"].ToString());

                            OutputAddressData[] response3 = client.GetAddressData(baseparams);

                            foreach (OutputAddressData resp in response3)
                            {
                                string nzp_dom = "";
                                ComparisonStatus stat = ComparisonStatus.NotMapped;
                                foreach (RosterCompareResult reqRes in response2)
                                {
                                    if (reqRes.Uid == resp.Id.ToString())
                                    {
                                        nzp_dom = reqRes.ExtId;
                                        stat = reqRes.ComparisonStatus;
                                        break;
                                    }
                                }

                                if (stat == ComparisonStatus.Mapped)
                                {                                    
                                    try
                                    {
                                        string sqlString = "SELECT * FROM ext_sys_addr_space where kod_sys = 1 and nzp_dom = " + nzp_dom;
                                        IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn, transactionID);
                                        bool update;
                                        if (dt.resultData.Rows.Count != 0)
                                        {
                                            update = true;
                                            sqlString = "UPDATE ext_sys_addr_space SET ( full_address, region_short_name, region_name, region_aoguid, zone_short_name, " +
                                                    " zone_name, zone_aoguid, city_short_name, city_name, city_aoguid, street_short_name, street_name, street_aoguid, " +
                                                    " house_number, house_guid, struct_number, korp_number, okato, post_code, latitude, longitude ) = ( " +
                                                    " ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? ) where kod_sys = 1 and nzp_dom = ? ";
                                        }
                                        else
                                        {
                                            update = false;
                                            sqlString = "INSERT INTO ext_sys_addr_space ( full_address, region_short_name, region_name, region_aoguid, zone_short_name, " +
                                                    " zone_name, zone_aoguid, city_short_name, city_name, city_aoguid, street_short_name, street_name, street_aoguid, " +
                                                    " house_number, house_guid, struct_number, korp_number, okato, post_code, latitude, longitude, nzp_dom, kod_sys ) VALUES ( " +
                                                    " ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? ) ";
                                        }
                                        IfxCommand ifxCommand = new IfxCommand(sqlString, conn) { Transaction = transactionID };
                                        IfxCommandQueryParam ifxPrm = new IfxCommandQueryParam(ifxCommand);

                                        ifxPrm.AddParam("full_address", resp.FullAddress);
                                        ifxPrm.AddParam("region_short_name", resp.RegionShortName);
                                        ifxPrm.AddParam("region_name", resp.RegionName);
                                        ifxPrm.AddParam("region_aoguid", resp.RegionAoguid);
                                        ifxPrm.AddParam("zone_short_name", resp.ZoneShortName);
                                        ifxPrm.AddParam("zone_name", resp.ZoneName);
                                        ifxPrm.AddParam("zone_aoguid", resp.ZoneAoguid);
                                        ifxPrm.AddParam("city_short_name", resp.CityShortName);
                                        ifxPrm.AddParam("city_name", resp.CityName);
                                        ifxPrm.AddParam("city_aoguid", resp.CityAoguid);
                                        ifxPrm.AddParam("street_short_name", resp.StreetShortName);
                                        ifxPrm.AddParam("street_name", resp.StreetName);
                                        ifxPrm.AddParam("street_aoguid", resp.StreetAoguid);
                                        ifxPrm.AddParam("house_number", resp.HouseNumber);
                                        ifxPrm.AddParam("house_guid", resp.HouseGuid);
                                        ifxPrm.AddParam("struct_number", resp.StructNumber);
                                        ifxPrm.AddParam("korp_number", resp.KorpNumber);
                                        ifxPrm.AddParam("okato", resp.Okato);
                                        ifxPrm.AddParam("post_code", resp.PostCode);
                                        ifxPrm.AddParam("latitude", resp.Latitude);
                                        ifxPrm.AddParam("longitude", resp.Longitude);
                                        ifxPrm.AddParam("nzp_dom", nzp_dom);
                                        if (!update)
                                        {
                                            ifxPrm.AddParam("kod_sys", "1");
                                        }

                                        ifxCommand.ExecuteNonQuery();
                                    }
                                    catch (Exception ex)
                                    {
                                        transactionID.Rollback();
                                        MessageBox.Show(ex.Message);
                                    }
                                }
                            }
                            start += 1000;
                        }
                    }
                    transactionID.Commit();
                            #endregion
                }
                MessageBox.Show("Выгрузка прошла успешно");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private DataTable ConvertToTitleCase(string field, DataTable dt)
        {
            dt.Columns.Add("socr");
            dt.Columns.Add("name");
            CultureInfo cultInf = Thread.CurrentThread.CurrentCulture;
            TextInfo txtInf = cultInf.TextInfo;
            foreach (DataRow row in dt.Rows)
            {
                row[field] = row[field].ToString().ToLower().Trim();
                row[field] = txtInf.ToTitleCase(row[field].ToString());
                string[] words = row[field].ToString().Split(' ');
                if (words.Length != 1)
                {
                    words[words.Length - 1] = words[words.Length - 1].ToLower();
                    row["socr"] = words[words.Length - 1];
                    string resString = "";
                    string strName = "";
                    foreach (string str in words)
                    {
                        resString += str + " ";
                    }
                    row[field] = resString.Trim();

                    for (int i = 0; i < words.Length - 1; i++)
                    {
                        strName += words[i] + " ";
                    }
                    row["name"] = strName.Trim();
                }
                else
                {
                    row["name"] = row[field];
                }
            }
            return dt;
        }
    }

    public class CloudRosterClientService : ICloudRosterClientService
    {
        /// <summary>
        /// Прием результатов согласования с НСИ
        /// </summary>
        /// <param name="login">Логин</param>
        /// <param name="pass">Пароль</param>
        /// <param name="compareData">Результаты согласования</param>
        public bool ReceiveCompareResults(string login, string pass, List<RosterCompareResult> compareData)
        {
            foreach (var comparison in compareData)
            {
                // сохранить результаты согласования
                var extId = comparison.ExtId;
                var uid = comparison.Uid;
                var state = comparison.ComparisonStatus;
                var reason = comparison.ManualCompareReason;
            }

            return true;
        }
    }
}


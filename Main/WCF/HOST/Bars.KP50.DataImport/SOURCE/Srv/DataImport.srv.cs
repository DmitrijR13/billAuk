using Bars.KP50.DataImport.SOURCE.COMPARE;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Bars.KP50.DataImport.SOURCE.Srv
{

    public class srv_DataImport : srv_Base, I_DataImport
    {
        public ReturnsObjectType<List<ComparedAreas>> GetComparedArea(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedAreas>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedArea(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();

                try
                {
                    ret = db.GetComparedArea(finder);


                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetComparedArea" + "\n" + ex.Message, MonitorLog.typelog.Error, 2,
                            100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedSupps>> GetComparedSupp(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedSupps>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedSupp(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedSupp(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetComparedSupp" + "\n" + ex.Message, MonitorLog.typelog.Error, 2,
                            100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedPayer>> GetComparedPayer(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedPayer>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedPayer(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedPayer(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetComparedPayer" + "\n" + ex.Message, MonitorLog.typelog.Error, 2,
                            100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedVills>> GetComparedMO(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedVills>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedMO(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedMO(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetComparedMO" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100,
                            true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedServs>> GetComparedServ(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedServs>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedServ(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedServ(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetComparedServ" + "\n" + ex.Message, MonitorLog.typelog.Error, 2,
                            100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedMeasures>> GetComparedMeasure(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedMeasures>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedMeasure(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedMeasure(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetComparedMeasure" + "\n" + ex.Message, MonitorLog.typelog.Error, 2,
                            100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParType(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedParType(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedParType(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetComparedParType" + "\n" + ex.Message, MonitorLog.typelog.Error, 2,
                            100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParBlag(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedParBlag(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedParBlag(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetComparedParBlag" + "\n" + ex.Message, MonitorLog.typelog.Error, 2,
                            100, true);
                }
                db.Close();
            }
            return ret;
        }


        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParGas(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedParGas(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedParGas(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedParGas" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParWater(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedParWater(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedParWater(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedParWater" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedTowns>> GetComparedTown(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedTowns>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedTown(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedTown(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedTown" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedRajons>> GetComparedRajon(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedRajons>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedRajon(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedRajon(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedRajon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedStreets>> GetComparedStreets(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedStreets>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedStreets(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedStreets(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedStreets" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedHouses>> GetComparedHouse(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedHouses>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedHouse(finder);
            }
            else
            {

                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedHouse(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedHouse" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedLS>> GetComparedLS(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedLS>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedLS(finder);
            }
            else
            {
                GetComparedData db = new GetComparedData();
                try
                {
                    ret = db.GetComparedLS(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedLS" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedAreas>> GetUncomparedArea(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedAreas>> ret = new ReturnsObjectType<List<UncomparedAreas>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedArea(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedArea(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedArea" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedSupps>> GetUncomparedSupp(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedSupps>> ret = new ReturnsObjectType<List<UncomparedSupps>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedSupp(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedSupp(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedSupp" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedPayer>> GetUncomparedPayer(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedPayer>> ret = new ReturnsObjectType<List<UncomparedPayer>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedPayer(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedPayer(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedPayer" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedVills>> GetUncomparedMO(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedVills>> ret = new ReturnsObjectType<List<UncomparedVills>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedMO(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedMO(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedMO" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedServs>> GetUncomparedServ(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedServs>> ret = new ReturnsObjectType<List<UncomparedServs>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedServ(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedServ(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedServ" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParType(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedParType(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedParType(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedParType" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParBlag(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedParBlag(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedParBlag(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedParBlag" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParGas(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedParGas(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedParGas(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedParGas" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParWater(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedParWater(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedParWater(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedParWater" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedMeasures>> GetUncomparedMeasure(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedMeasures>> ret = new ReturnsObjectType<List<UncomparedMeasures>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedMeasure(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedMeasure(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedMeasure" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedTowns>> GetUncomparedTown(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedTowns>> ret = new ReturnsObjectType<List<UncomparedTowns>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedTown(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedTown(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedTown" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedRajons>> GetUncomparedRajon(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedRajons>> ret = new ReturnsObjectType<List<UncomparedRajons>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedRajon(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedRajon(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedRajon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedStreets>> GetUncomparedStreets(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedStreets>> ret = new ReturnsObjectType<List<UncomparedStreets>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedStreets(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedStreets(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedStreets" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedHouses>> GetUncomparedHouse(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedHouses>> ret = new ReturnsObjectType<List<UncomparedHouses>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedHouse(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedHouse(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedHouse" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedLS>> GetUncomparedLS(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedLS>> ret = new ReturnsObjectType<List<UncomparedLS>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedLS(finder);
            }
            else
            {
                GetUncomparedData db = new GetUncomparedData();
                try
                {
                    ret = db.GetUncomparedLS(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedLS" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkArea(UncomparedAreas finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkArea(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkArea(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkArea" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkSupp(UncomparedSupps finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkSupp(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkSupp(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkSupp" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkPayer(UncomparedPayer finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkPayer(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkPayer(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkPayer" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkMO(UncomparedVills finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkMO(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkMO(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkMO" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkServ(UncomparedServs finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkServ(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkServ(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkServ" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkParType(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkParType(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkParType(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkParType" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkParBlag(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkParBlag(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkParBlag(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkParBlag" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkParGas(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkParGas(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkParGas(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkParGas" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkParWater(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkParWater(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkParWater(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkParWater" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkMeasure(UncomparedMeasures finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkMeasure(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkMeasure(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkMeasure" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkTown(UncomparedTowns finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkTown(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkTown(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkTown" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkRajon(UncomparedRajons finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkRajon(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkRajon(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkRajon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkNzpStreet(UncomparedStreets finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkNzpStreet(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkStreet(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkStreet" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkNzpDom(UncomparedHouses finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkNzpDom(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkDom(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkDom" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkLS(UncomparedLS finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkLS(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkLs(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkLs" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewArea(UncomparedAreas finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewArea(finder, selectedFiles);
            }
            else
            {
                AddNewData db = new AddNewData();
                try
                {
                    ret = db.AddNewArea(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewArea" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewSupp(UncomparedSupps finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewSupp(finder, selectedFiles);
            }
            else
            {
                AddNewData db = new AddNewData();
                try
                {
                    ret = db.AddNewSupp(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewSupp" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewPayer(UncomparedPayer finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewPayer(finder, selectedFiles);
            }
            else
            {
                AddNewData db = new AddNewData();
                try
                {
                    ret = db.AddNewPayer(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewPayer" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewMO(UncomparedVills finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewMO(finder, selectedFiles);
            }
            else
            {
                AddNewData db = new AddNewData();

                try
                {
                    ret = db.AddNewMO(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewMO" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewServ(UncomparedServs finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewServ(finder, selectedFiles);
            }
            else
            {
                AddNewData db = new AddNewData();
                try
                {
                    ret = db.AddNewServ(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewServ" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewParType(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewParType(finder, selectedFiles);
            }
            else
            {
                AddNewData db = new AddNewData();
                try
                {
                    ret = db.AddNewParType(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewParType" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewParBlag(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewParBlag(finder, selectedFiles);
            }
            else
            {
                AddNewData db = new AddNewData();
                try
                {
                    ret = db.AddNewParBlag(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewParBlag" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }


        public ReturnsType AddNewParGas(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewParGas(finder, selectedFiles);
            }
            else
            {
                AddNewData db = new AddNewData();
                try
                {
                    ret = db.AddNewParGas(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewParGas" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewParWater(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewParWater(finder, selectedFiles);
            }
            else
            {
                AddNewData db = new AddNewData();
                try
                {
                    ret = db.AddNewParWater(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewParWater" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewMeasure(UncomparedMeasures finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewMeasure(finder, selectedFiles);
            }
            else
            {
                AddNewData db = new AddNewData();
                try
                {
                    ret = db.AddNewMeasure(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewMeasure" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewRajon(UncomparedRajons finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewRajon(finder, selectedFiles);
            }
            else
            {
                AddNewData db = new AddNewData();
                try
                {
                    ret = db.AddNewRajon(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewRajon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewStreet(UncomparedStreets finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewStreet(finder, selectedFiles);
            }
            else
            {
                AddNewData db = new AddNewData();
                try
                {
                    ret = db.AddNewStreet(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewStreet" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewHouse(UncomparedHouses finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewHouse(finder, selectedFiles);
            }
            else
            {
                AddNewData db = new AddNewData();
                try
                {
                    ret = db.AddNewHouse(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewHouse" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns AddAllHouse(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddAllHouse(finder);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.AddAllHouse(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddAllHouse" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns LinkHouseOnly(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkHouseOnly(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkHouseOnly(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkHouseOnly" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns LinkLSAutom(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkLSAutom(finder);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkLsAutom(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkLsAutom" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns LinkEmptyStreetAutom(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkEmptyStreetAutom(finder);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkEmptyStreetAutom(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkEmptyStreetAutom" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns StopRefresh()
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.StopRefresh();
            }
            else
            {
                #region Подключение к БД

                IDbConnection con_db = DBManager.GetConnection(STCLINE.KP50.Global.Constants.cons_Kernel);
                var t = DBManager.OpenDb(con_db, true);
                if (!t.result)
                {
                    ret = new Returns() {result = false};
                    ret.text = "Ошибка при открытии соединения.";
                    ret.tag = -1;
                    MonitorLog.WriteLog(
                        "Ошибка при открытии соединения в ф-ции GetUploadingProgress" + Environment.NewLine + t.text,
                        MonitorLog.typelog.Error, 2, 100, true);
                    return ret;
                }

                #endregion Подключение к БД

                DbKladr db = new DbKladr(con_db);
                try
                {
                    ret = db.StopRefresh();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка " + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns LinkPayerWithAdd(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkPayerWithAdd(finder);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkPayerWithAdd(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkPayerWithAdd" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        public Returns LinkPayerAutom(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkPayerAutom(finder);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkPayerAutom(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkPayerAutom" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        public Returns LinkParAutomWithAdd(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkParAutomWithAdd(finder);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkParAutomWithAdd(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkParAutomWithAdd" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        public Returns LinkParamsAutom(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkParamsAutom(finder);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkParamsAutom(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkParamsAutom" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        public Returns LinkStreetAutom(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkStreetAutom(finder);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkStreetAutom(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkStreetAutom" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns LinkRajonAutom(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkRajonAutom(finder);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkRajonAutom(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkRajonAutom" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }



        public ReturnsType UnlinkArea(ComparedAreas finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkArea(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkArea(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkArea" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkSupp(ComparedSupps finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkSupp(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkSupp(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkSupp" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkPayer(ComparedPayer finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkPayer(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkPayer(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkPayer" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkMO(ComparedVills finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkMO(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkMO(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkMO" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkServ(ComparedServs finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkServ(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkServ(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkServ" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkParType(ComparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkParType(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkParType(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkParType" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkParBlag(ComparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkParBlag(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkParBlag(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkParBlag" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkParGas(ComparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkParGas(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkParGas(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkParGas" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkParWater(ComparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkParWater(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkParWater(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkParWater" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkMeasure(ComparedMeasures finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkMeasure(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkMeasure(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkMeasure" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkTown(ComparedTowns finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkTown(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkTown(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkTown" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkRajon(ComparedRajons finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkRajon(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkRajon(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkRajon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkStreet(ComparedStreets finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkStreet(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkStreet(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkStreet" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkHouse(ComparedHouses finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkHouse(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkHouse(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkHouse" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkLS(ComparedLS finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkLS(finder, selectedFiles);
            }
            else
            {
                UnlinkData db = new UnlinkData();
                try
                {
                    ret = db.UnlinkLS(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkLS" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }


        public ReturnsObjectType<List<UncomparedAreas>> GetAreaByFilter(UncomparedAreas finder)
        {
            ReturnsObjectType<List<UncomparedAreas>> ret = new ReturnsObjectType<List<UncomparedAreas>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetAreaByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetAreaByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetAreaByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedSupps>> GetSuppByFilter(UncomparedSupps finder)
        {
            ReturnsObjectType<List<UncomparedSupps>> ret = new ReturnsObjectType<List<UncomparedSupps>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetSuppByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetSuppByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSuppByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedPayer>> GetPayerByFilter(UncomparedPayer finder)
        {
            ReturnsObjectType<List<UncomparedPayer>> ret = new ReturnsObjectType<List<UncomparedPayer>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetPayerByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetPayerByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetPayerByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedVills>> GetMOByFilter(UncomparedVills finder)
        {
            ReturnsObjectType<List<UncomparedVills>> ret = new ReturnsObjectType<List<UncomparedVills>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetMOByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetMOByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetMOByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedServs>> GetServByFilter(UncomparedServs finder)
        {
            ReturnsObjectType<List<UncomparedServs>> ret = new ReturnsObjectType<List<UncomparedServs>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetServByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetServByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetServByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParTypeByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetParTypeByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetParTypeByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetParTypeByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParBlagByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetParBlagByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetParBlagByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetParBlagByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParGasByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetParGasByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetParGasByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetParGasByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParWaterByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetParWaterByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetParWaterByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetParWaterByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedMeasures>> GetMeasureByFilter(UncomparedMeasures finder)
        {
            ReturnsObjectType<List<UncomparedMeasures>> ret = new ReturnsObjectType<List<UncomparedMeasures>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetMeasureByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetMeasureByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetMeasureByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedTowns>> GetTownByFilter(UncomparedTowns finder)
        {
            ReturnsObjectType<List<UncomparedTowns>> ret = new ReturnsObjectType<List<UncomparedTowns>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetTownByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetTownByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetTownByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedRajons>> GetRajonByFilter(UncomparedRajons finder)
        {
            ReturnsObjectType<List<UncomparedRajons>> ret = new ReturnsObjectType<List<UncomparedRajons>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetRajonByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetRajonByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetRajonByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedStreets>> GetStreetsByFilter(UncomparedStreets finder)
        {
            ReturnsObjectType<List<UncomparedStreets>> ret = new ReturnsObjectType<List<UncomparedStreets>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetStreetsByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetStreetsByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetStreetsByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedHouses>> GetHouseByFilter(UncomparedHouses finder)
        {
            ReturnsObjectType<List<UncomparedHouses>> ret = new ReturnsObjectType<List<UncomparedHouses>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetHouseByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetHouseByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetHouseByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedLS>> GetLsByFilter(UncomparedLS finder)
        {
            ReturnsObjectType<List<UncomparedLS>> ret = new ReturnsObjectType<List<UncomparedLS>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetLsByFilter(finder);
            }
            else
            {
                GetDataByFilter db = new GetDataByFilter();
                try
                {
                    ret = db.GetLsByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetLsByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType ChangeTownForRajon(UncomparedRajons finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.ChangeTownForRajon(finder, selectedFiles);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.ChangeTownForRajon(finder, selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ChangeTownForRajon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }


        public Returns MakePacks(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.MakePacks(finder);
            }
            else
            {
                #region Подключение к БД
                IDbConnection con_db = DBManager.GetConnection(STCLINE.KP50.Global.Constants.cons_Kernel);
                if (!DBManager.OpenDb(con_db, true).result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции MakePacks ", MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка при открытии соединения.";
                    ret.result = false;
                    ret.tag = -1;
                    return ret;
                }
                #endregion Подключение к БД

                var db = new DBMakePacks(con_db);
                try
                {
                    ret = db.MakePacks(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции!";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка выполнения процедуры MakePacks" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }


        public Returns UsePreviousLinks(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UsePreviousLinks(finder);
            }
            else
            {
                var db = new DbUsePreviousLinks();
                try
                {
                    ret = db.UsePreviousLinks(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции!";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка выполнения процедуры UsePreviousLinks" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }


        public Returns MakeReportOfLoad(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.MakeReportOfLoad(finder);
            }
            else
            {
                var db = new LoadReport();
                try
                {
                    ret = db.MakeReportOfLoad(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции!";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка выполнения процедуры MakeReportOfLoad" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public List<int> GetFilesIsNotExistAct(FilesImported finder, out Returns ret)
        {
            ret = new Returns();
            var listInt = new List<int>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                listInt = cli.GetFilesIsNotExistAct(finder, out ret);
            }
            else
            {
                var db = new LoadReport();
                try
                {
                    listInt = db.GetFilesIsNotExistAct(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции!";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка выполнения процедуры GetFilesIsNotExistAct" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return listInt;
        }


        public ReturnsType DbSaveFileToDisassembly(FilesImported finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DbSaveFileToDisassembly(finder);
            }
            else
            {
                #region Подключение к БД
                IDbConnection con_db = DBManager.GetConnection(STCLINE.KP50.Global.Constants.cons_Kernel);
                if (!DBManager.OpenDb(con_db, true).result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции DbSaveFileToDisassembly ", MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка при открытии соединения.";
                    ret.result = false;
                    ret.tag = -1;
                    return ret;
                }
                #endregion Подключение к БД

                DbDisassembleFile db = new DbDisassembleFile(con_db);
                try
                {
                    ret = db.DbSaveFileToDisassembly(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка DbSaveFileToDisassembly" + "\n" + ex.Message,
                            MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                    con_db.Close();
                }
            }
            return ret;
        }

        public Returns GetNzpFileLoad(FilesImported finder)
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetNzpFileLoad(finder);
            }
            else
            {
                #region Подключение к БД
                IDbConnection con_db = DBManager.GetConnection(STCLINE.KP50.Global.Constants.cons_Kernel);
                if (!DBManager.OpenDb(con_db, true).result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции DbSaveFileToDisassembly ", MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка при открытии соединения.";
                    ret.result = false;
                    ret.tag = -1;
                    return ret;
                }
                #endregion Подключение к БД

                DbDisassembleFile db = new DbDisassembleFile(con_db);
                try
                {
                    ret = db.GetNzpFileLoad(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetNzpFileLoad" + "\n" + ex.Message,
                            MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                    con_db.Close();
                }
            }
            return ret;
        }



        public ReturnsObjectType<List<FilesImported>> GetFiles(FilesImported finder)
        {
            ReturnsObjectType<List<FilesImported>> ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFiles(finder);
            }
            else
            {
                DataImportUtils db = new DataImportUtils();
                try
                {
                    Returns r;
                    ret = new ReturnsObjectType<List<FilesImported>>(db.GetFiles(finder, out r));
                    ret.tag = r.tag;
                }
                catch (Exception ex)
                {
                    ret = new ReturnsObjectType<List<FilesImported>>() {result = false};
                    ret.text = "Ошибка вызова функции загрузки файлов в БД";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetFiles" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100,
                            true);
                }
                finally
                {
                    db.Close();
                }
            }
            return ret;
        }



        public Returns OperateWithFileImported(FilesDisassemble finder, FilesImportedOperations operation)
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.OperateWithFileImported(finder, operation);
            }
            else
            {
                #region Подключение к БД
                IDbConnection con_db = DBManager.GetConnection(STCLINE.KP50.Global.Constants.cons_Kernel);
                ret = DBManager.OpenDb(con_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции OperateWithFileImported ",MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка открытия соединения!";
                    return ret;
                }
                #endregion Подключение к БД

                DbDeleteImportedFile delFile = new DbDeleteImportedFile(con_db);

                try
                {
                    switch (operation)
                    {
                        case FilesImportedOperations.Delete:
                            ret = delFile.DeleteImportedFile(finder);
                            break;
                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }

                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка при удалении файла!";
                    MonitorLog.WriteLog("Ошибка OperateWithFileImported\n" + ex.Message, MonitorLog.typelog.Error, 2,
                        100, true);
                }
                finally
                {
                    delFile.Close();
                    con_db.Close();
                }
            }
            return ret;
        }

        public Returns RefreshKLADRFile(FilesImported finder)
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.RefreshKLADRFile(finder);
            }
            else
            {
                #region Подключение к БД
                IDbConnection con_db = DBManager.GetConnection(STCLINE.KP50.Global.Constants.cons_Kernel);
                if (!DBManager.OpenDb(con_db, true).result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции CalcProc " +
                                        "при  при обработке задачи разбора файла (taskLoadKladr)",
                        MonitorLog.typelog.Error, true);
                    return ret;
                }
                #endregion Подключение к БД

                DbKladr db = new DbKladr(con_db);
                try
                {
                    ret = db.RefreshKLADRFile(finder, ref ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка RefreshKLADRFile" + "\n" + ex.Message, MonitorLog.typelog.Error, 2,
                            100, true);
                }
                finally
                {
                    db.Close();
                    con_db.Close();
                }
            }
            return ret;
        }

        public ReturnsObjectType<List<KLADRData>> LoadDataFromKLADR(KLADRFinder finder)
        {
            ReturnsObjectType<List<KLADRData>> ret = new ReturnsObjectType<List<KLADRData>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LoadDataFromKLADR(finder);
            }
            else
            {
                #region Подключение к БД
                IDbConnection con_db = DBManager.GetConnection(STCLINE.KP50.Global.Constants.cons_Kernel);
                var t = DBManager.OpenDb(con_db, true);
                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции LoadDataFromKLADR: " + t.text,MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = t.text;
                    ret.tag = -1;
                    return ret;
                }
                #endregion Подключение к БД

                DbKladr db = new DbKladr(con_db);
                try
                {
                    ret = db.LoadDataFromKLADR(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка LoadRegionKLADR" + "\n" + ex.Message, MonitorLog.typelog.Error, 2,
                            100, true);
                }
                finally
                {
                    con_db.Close();
                }

            }
            return ret;
        }

        public ReturnsObjectType<List<UploadingData>> GetUploadingProgress(UploadingData finder)
        {
            ReturnsObjectType<List<UploadingData>> ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUploadingProgress(finder);
            }
            else
            {
                #region Подключение к БД
                IDbConnection con_db = DBManager.GetConnection(STCLINE.KP50.Global.Constants.cons_Kernel);
                var t = DBManager.OpenDb(con_db, true);
                if (!t.result)
                {
                    ret = new ReturnsObjectType<List<UploadingData>>() { result = false };
                    ret.text = "Ошибка при открытии соединения.";
                    ret.tag = -1;
                    MonitorLog.WriteLog("Ошибка при открытии соединения в ф-ции GetUploadingProgress" + Environment.NewLine + t.text, MonitorLog.typelog.Error, 2, 100, true);
                    return ret;
                }
                #endregion Подключение к БД
                DbKladr db = new DbKladr(con_db);
                try
                {
                    Returns r;
                    ret = new ReturnsObjectType<List<UploadingData>>(db.GetUploadingProgress(finder, out r));
                    ret.tag = r.tag;
                }
                catch (Exception ex)
                {
                    ret = new ReturnsObjectType<List<UploadingData>>() {result = false};
                    ret.text = "Ошибка вызова функции загрузки файлов в БД";
                    ret.tag = -1;
                    ////if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetUploadingProgress" + "\n" + ex.Message, MonitorLog.typelog.Error,
                            2, 100, true);
                }
                finally
                {
                    db.Close();
                    con_db.Close();
                }
            }
            return ret;
        }


        public Returns MakeUniquePayer(List<int> selectedFiles)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.MakeUniquePayer(selectedFiles);
            }
            else
            {
                PayerUnique db = new PayerUnique();
                try
                {
                    ret = db.MakeUniquePayer(selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка MakeUniquePayer" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns MakeNonUniquePayer(List<int> selectedFiles)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.MakeNonUniquePayer(selectedFiles);
            }
            else
            {
                PayerUnique db = new PayerUnique();
                try
                {
                    ret = db.MakeNonUniquePayer(selectedFiles);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка MakeUniquePayer" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns LinkServiceAuto(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_DataImport cli = new cli_DataImport(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkServiceAuto(finder);
            }
            else
            {
                LinkData db = new LinkData();
                try
                {
                    ret = db.LinkServiceAuto(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkServiceAuto" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }
    }
}

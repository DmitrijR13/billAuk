
using System.Data;
using System.IO;
using Microsoft.SqlServer.Server;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Exchange.Unload
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using STCLINE.KP50.Interfaces;
    using STCLINE.KP50.Global;


    public class RegisterSupplData : BaseUnload20
    {
        public override string Name
        {
            get { return "RegisterSupplData"; }
        }

        public override string NameText
        {
            get { return "Реестр поставщиков данных"; }
        }

        public override int Code
        {
            get { return 1; }
        }

       public override List<FieldsUnload> Data { get; set; }


       public override void Start()
        {

        }

        public override void Start(string pref)
        {
            Data = new List<FieldsUnload>();
            try
            {
                OpenConnection();
                CreateTempTable();
                InsertData(pref);

                #region выборка данных из временной таблицы

                string sql = "Select * "
                             + " from " + Name;

                IDataReader reader;
                ExecRead(out reader, sql);

                var curtime = new DateTime(Year, Month, 1);

                while (reader.Read())
                {
                    var version = new Field
                    {
                        N = "Version",
                        NT = "Номер выгрузки",
                        IS = 1,
                        P = 1,
                        T = "TextType",
                        L = 25,
                        V = (reader["Version"] != DBNull.Value) ? Convert.ToString(reader["Version"]).Trim() : ""
                    };
                    var loadData = new Field
                    {
                        N = "LoadDate",
                        NT = "Дата загрузки",
                        IS = 1,
                        P = 2,
                        T = "DateType",
                        L = 0,
                        V = DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")
                    };
                    var nameSender = new Field
                    {
                        N = "NameSender",
                        NT = "Наименование организации-отправителя",
                        IS = 1,
                        P = 3,
                        T = "TextType",
                        L = 150,
                        V = (reader["NameSender"] != DBNull.Value) ? Convert.ToString(reader["NameSender"]).Trim() : ""
                    };
                    var inn = new Field
                    {
                        N = "INN",
                        NT = "ИНН организации- отправителя",
                        IS = 0,
                        P = 4,
                        T = "IntType",
                        L = 15,
                        V = (reader["INN"] != DBNull.Value) ? Convert.ToString(reader["INN"]).Trim() : ""
                    };
                    var kpp = new Field
                    {
                        N = "KPP",
                        NT = "КПП организации-отправителя",
                        IS = 0,
                        P = 5,
                        T = "IntType",
                        L = 15,
                        V = (reader["KPP"] != DBNull.Value) ? Convert.ToString(reader["KPP"]).Trim() : ""
                    };
                    var subunitSender = new Field
                    {
                        N = "SubunitSender",
                        NT = "Подразделение организации- отправителя",
                        IS = 0,
                        P = 6,
                        T = "TextType",
                        L = 150,
                        V =
                            (reader["SubunitSender"] != DBNull.Value)
                                ? Convert.ToString(reader["SubunitSender"]).Trim()
                                : ""
                    };
                    var innSubunit = new Field
                    {
                        N = "INNSubunit",
                        NT = "ИНН подразделения организации отправителя",
                        IS = 0,
                        P = 7,
                        T = "IntType",
                        L = 15,
                        V = (reader["INNSubunit"] != DBNull.Value) ? Convert.ToString(reader["INNSubunit"]).Trim() : ""
                    };
                    var fileNumber = new Field
                    {
                        N = "FileNumber",
                        NT = "№ файла",
                        IS = 1,
                        P = 8,
                        T = "TextType",
                        L = 25,
                        V = BlaBlaBla25()
                    };
                    var dateFile = new Field
                    {
                        N = "DateFile",
                        NT = "Дата файла",
                        IS = 1,
                        P = 9,
                        T = "DateType",
                        L = 0,
                        V = DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")
                    };
                    var telefone = new Field
                    {
                        N = "Telefone",
                        NT = "Телефон отправителя",
                        IS = 1,
                        P = 10,
                        T = "TextType",
                        L = 25,
                        V = (reader["Telefone"] != DBNull.Value) ? Convert.ToString(reader["Telefone"]).Trim() : ""
                    };
                    var snpSender = new Field
                    {
                        N = "SNPSender",
                        NT = "ФИО отправителя",
                        IS = 1,
                        P = 11,
                        T = "TextType",
                        L = 50,
                        V = (reader["SNPSender"] != DBNull.Value) ? Convert.ToString(reader["SNPSender"]).Trim() : ""
                    };
                    var chargeDate = new Field
                    {
                        N = "ChargeDate",
                        NT = "Месяц и год начисления",
                        IS = 1,
                        P = 12,
                        T = "DateType",
                        L = 0,
                        V = curtime
                            .ToString("dd.MM.yyyy hh:mm:ss")
                    };
                    var formatVersion = new Field
                    {
                        N = "FormatVersion",
                        NT = "Версия формата",
                        IS = 1,
                        P = 13,
                        T = "TextType",
                        L = 25,
                        V = "1.0"
                    };
                    var gorodCode = new Field
                    {
                        N = "GorodCode", 
                        NT = "Код в ФСГ \"Город\"",
                        IS = 1, P = 14,
                        T = "TextType",
                        L = 30, 
                        V = (reader["GorodCode"] != DBNull.Value ? reader["GorodCode"].ToString().Trim() : "")
                    };

                    var fields = new FieldsUnload
                    {
                        F =
                            new List<Field>
                            {
                                version,
                                loadData,
                                nameSender,
                                inn,
                                kpp,
                                subunitSender,
                                innSubunit,
                                fileNumber,
                                dateFile,
                                telefone,
                                snpSender,
                                chargeDate,
                                formatVersion,
                                gorodCode
                            }
                    };
                    Data.Add(fields);
                }


                #endregion
            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("Ошибка выгрузки секции " + Name + ":" + ex, MonitorLog.typelog.Error, true);
            }
            DropTempTable();
            CloseConnection();
        }

        public override void StartSelect()
        {   
        }

        private void InsertData(string pref)
        {
            string unlDate = " CAST( '" + Year + "-" + Month + "-01' as DATE) ";

            string dat_s = " CAST( '" + Year + "-" + Month.ToString("00") + "-01' as DATE) ";
            int dayInMonth = DateTime.DaysInMonth(Year, Month);
            string dat_po = " CAST( '" + Year + "-" + Month.ToString("00") + "-" + dayInMonth +
                            "' as DATE) ";
            string RegSuppTblName = Name;

            string sql =
                " INSERT INTO " + RegSuppTblName + "                  " +
                " (                                                     " +
               // " 	Version, " +
//                "   LoadDate,  " +
//                " 	FileNumber,         " +
//                " 	DateFile,             " +
                  " SNPSender  " +
//                " 	ChargeDate ,                " +
//                " 	FormatVersion                 " +
                " )                         " +
                " SELECT  " 
//                +" 1 , "
//                + "'" + DateTime.Now.ToShortDateString() + "', "  
//                +BlaBlaBla25()
//                +",'" + DateTime.Now.ToShortDateString()+"', "
                  + " u.uname"
//                + unlDate
//                +", '1.0' "
                + " FROM " + DBManager.sDefaultSchema + "users u " 
                + " WHERE u.nzp_user=" + NzpUser;
            ExecSQL(sql);

            sql= " UPDATE " + RegSuppTblName + " set NameSender= (SELECT val_prm FROM "+ pref + DBManager.sDataAliasRest+"prm_10 p"
               + " WHERE p.nzp_prm=80 AND is_actual = 1 AND dat_s<=" + dat_po + "AND dat_po>=" + dat_s + "),"
               + " Telefone=(SELECT val_prm FROM "+ pref + DBManager.sDataAliasRest+"prm_10 p"
               + " WHERE p.nzp_prm=96 AND is_actual = 1 AND dat_s<=" + dat_po + "AND dat_po>=" + dat_s + "), "
               + " INN = (SELECT inn::bigint FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_payer where nzp_payer = (SELECT max(nzp_payer_agent) FROM "
               + Points.Pref + DBManager.sKernelAliasRest + "supplier ) ), "
               + " GorodCode = (SELECT max(val_prm) FROM " + Points.Pref + DBManager.sDataAliasRest + "prm_9 pr " +
               " WHERE pr.nzp_prm = 1420 AND pr.is_actual = 1 AND  pr.dat_s <= " + dat_po + " AND pr.dat_po >= " + dat_s + ")";
            ExecSQL(sql);

            CheckColumnOnEmptiness("INN", "Поле \"ИНН организации-отправителя\" в секции \"Реестр поставщиков данных\" не заполнено");

        }  

        public override void CreateTempTable()
        {
            string columnNames =
                " id SERIAL, " +
                " TypeSection	 INTEGER , " +
                " Name	 CHAR (50), " +
                " Version	 CHAR (25), " +
                " LoadDate	 DATE 	, " +
                " NameSender	 CHAR (150), " +
                " INN	BIGINT, " +
                " KPP	INTEGER, " +
                " SubunitSender	 CHAR (150), " +
                " INNSubunit	INTEGER, " +
                " FileNumber	 CHAR (25), " +
                " DateFile	 DATE 	, " +
                " Telefone	 CHAR (25), " +
                " SNPSender	 CHAR (50), " +
                " ChargeDate	 DATE 	, " +
                " FormatVersion	 CHAR (25)," +
                " GorodCode     CHAR(30)";

            ExecSQL(" DROP TABLE " + Name, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", Name, columnNames));
        }

        private string BlaBlaBla25()    //номер файла нужно сделать
        {
            return DateTime.Now.ToString("yyyyMMddHH");
        }

    }
}

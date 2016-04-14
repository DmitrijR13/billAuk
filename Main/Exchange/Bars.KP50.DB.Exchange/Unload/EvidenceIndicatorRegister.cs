using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DB.Exchange.Unload
{
    public class EvidenceIndicatorRegister : BaseUnload20
    {
      
        public override string Name
        {
            get { return "EvidenceIndicatorRegister"; }
        }

        public override string NameText
        {
            get { return "Показания прибора учета"; }
        }

        public override int Code
        {
            get { return 12; }
        }

        public override List<FieldsUnload> Data { get; set; }

        public override void Start(string pref)
        {
            Data = new List<FieldsUnload>();
            OpenConnection();
            CreateTempTable();
            FillEvidenceIndicatorRegister(pref);

            try
            {

            string sql = " SELECT * FROM EvidenceIndicatorRegister ";

            

            IDataReader reader;

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                #region заполнение полей секции
                var indicatorIdSystem = new Field
                {
                    N = "IndicatorIDSystem",
                    NT = "Уникальный код прибора учета в системе отправителя",
                    IS = 1,
                    P = 1,
                    T = "TextType",
                    L = 20,
                    V =
                        (reader["IndicatorIDSystem"] != DBNull.Value)
                            ? Convert.ToString(reader["IndicatorIDSystem"]).Trim()
                            : ""
                };
                var volumeType = new Field
                {
                    N = "VolumeType",
                    NT = "Тип расхода",
                    IS = 1,
                    P = 2,
                    T = "IntType",
                    L = 1,
                    V =
                        (reader["VolumeType"] != DBNull.Value)
                            ? Convert.ToString(reader["VolumeType"]).Trim()
                            : ""
                };
                var date = new Field
                {
                    N = "TDateOrTMonth",
                    NT = "Тип расхода",
                    IS = 1,
                    P = 3,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["TDate"] != DBNull.Value)
                            ? Convert.ToString(reader["TDate"]).Trim()
                            : ""
                };
                var puValues = new Field
                {
                    N = "PUValues",
                    NT = "Показание прибора учета / Месячный расход",
                    IS = 1,
                    P = 4,
                    T = "DecimalType",
                    L = 40,
                    V =
                        (reader["PUValues"] != DBNull.Value)
                            ? Convert.ToString(reader["PUValues"]).Trim()
                            : ""
                };
                var unitId = new Field
                {
                    N = "UnitID",
                    NT = "Код услуги",
                    IS = 0,
                    P = 5,
                    T = "IntType",
                    L = 3,
                    V =
                        (reader["UnitID"] != DBNull.Value)
                            ? Convert.ToString(reader["UnitID"]).Trim()
                            : ""
                };

                var fields = new FieldsUnload
                {
                    F = new List<Field>
                    {
                      indicatorIdSystem,
                      volumeType,
                      date,
                      puValues,
                      unitId
                    }
                };
                #endregion

                Data.Add(fields);
            }

            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("EvidenceIndicatorRegister.Start(): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally 
            {
                DropTempTable();
            CloseConnection();
        }

        }

        public override void Start()
        {
        }
        public override void StartSelect()
        {
        }

        public override void CreateTempTable()
        {
            string columnNames =
                " id SERIAL, " +
                 " IndicatorIDSystem	 CHAR (20), " +
                " VolumeType	 INTEGER , " +
                " TDate	 DATE 	, " +
                " TMonth	 DATE 	, " +
                " PUValues	" + DBManager.sDecimalType + " (14,2) , " +
                " UnitID	 INTEGER  ";

            ExecSQL(" DROP TABLE " + Name, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", Name, columnNames));
        }

        public override void DropTempTable()
        {
        }

        /// <summary>
        ///  Заполнение показаний приборов учета всех типов
        /// </summary>
        /// <param name="pref">Префикс банка, из которого выгружаем</param>
        private void FillEvidenceIndicatorRegister(string pref)
        {
            //заполнить реестр показаний ОДПУ
            FillEvidence(pref, "counters_dom", 1);
            //заполнить реестр показаний ИПУ 
            FillEvidence(pref, "counters", 3);
        }

        /// <summary>
        /// Заполнение показаний приборов учета определенного типа
        /// </summary>
        /// <param name="pref">Префикс банка, из которого выгружаем</param>
        /// <param name="evidenceTableName">Название таблицы с показаниями определенного типа ПУ в Биллинге</param>
        /// <param name="nzp_type">Тип прибора учета в Биллинге(1-ОДПУ, 2-ГрПУ, 3 - ИПУ, 4-ОбщеквартирПУ)</param>
        private void FillEvidence(string pref, string evidenceTableName, int nzp_type)
        {
            string unlDate = " CAST( '" + Year + "-" + Month + "-01' as DATE) ";

            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            string loj = "";
            if (area != String.Empty)
            {
                switch (evidenceTableName)
                {
                    case "counters_dom" :
                        loj = " left outer join " + pref + DBManager.sDataAliasRest +
                              " dom d on cnt.nzp_dom = d.nzp_dom ";
                        whereArea = " and d.nzp_area in (" + area + " )";
                        break;
                    case "counters" :
                        loj = " left outer join " + pref + DBManager.sDataAliasRest +
                              " kvar k on cnt.nzp_kvar = k.nzp_kvar ";
                        whereArea = " and k.nzp_area in (" + area + " )";
                        break;
                }
            }

            string sql =
                " INSERT INTO EvidenceIndicatorRegister( " +
                " IndicatorIDSystem, " +
                " VolumeType, " +
                " TDate, " +
                " PUValues, " +
                " UnitID) " +
                " SELECT  " +
                " cnt.nzp_counter AS IndicatorIDSystem, " +
                //признак выгрузки показаний
                " 1 AS VolumeType,  " +
                " cnt.dat_uchet AS TDate, " +
                " cnt.val_cnt AS PUValues, " +
                " cnt.nzp_serv AS UnitID  " +
                " FROM " + pref + DBManager.sDataAliasRest + evidenceTableName + " cnt " + loj +
                " WHERE nzp_counter in  " +
                " (SELECT nzp_counter  " +
                " FROM " + pref + DBManager.sDataAliasRest + "counters_spis  " +
                //признак типа ПУ
                " WHERE nzp_type = " + nzp_type + ") " +
                " AND cnt.dat_uchet >= " + unlDate + " " +
                " AND cnt.dat_uchet <= " + unlDate + " + '1 month'::interval  " +
                " AND cnt.is_actual <> 100 " + whereArea;

            ExecSQL(sql);
        }
    }
}
